using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class FlyToPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Speed at which the object flies toward the player")]
    public float moveSpeed = 5f;

    [Tooltip("How fast the object rotates to face its movement direction")]
    public float rotationSpeed = 3f;

    [Tooltip("Time it takes to reach full speed")]
    public float accelerationTime = 1f;

    [Tooltip("Curve that controls how speed increases over time")]
    public AnimationCurve accelerationCurve;

    [Header("Target Settings")]
    [Tooltip("Whether to automatically find and target the player on start")]
    public bool findPlayerOnStart = true;

    [Tooltip("Tag used to find the player object")]
    public string playerTag = "Player";

    [Header("Behavior Settings")]
    [Tooltip("Maximum lifetime in seconds before self-destruction")]
    public float maxLifetime = 15f;

    [Tooltip("Whether to destroy this object when it hits the player")]
    public bool destroyOnPlayerCollision = true;

    [Tooltip("Effect to spawn when hitting the player")]
    public GameObject hitEffect;

    // References
    private Rigidbody rb;
    private Collider projectileCollider;
    private float currentSpeed;
    private float accelerationProgress = 0f;
    private bool isInitialized = false;
    private DamagingRock damagingRock;
    private static Transform cachedPlayerTransform;

    // The fixed target position (player's position at spawn time)
    private Vector3 targetPosition;
    private Vector3 flyDirection;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        projectileCollider = GetComponent<Collider>();
        damagingRock = GetComponent<DamagingRock>();

        // Make sure rigidbody is set up correctly
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent physics rotation

        // Set default acceleration curve if none provided
        if (accelerationCurve == null || accelerationCurve.length == 0)
        {
            accelerationCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        }
    }

    private void Start()
    {
        if (findPlayerOnStart)
        {
            FindPlayerOnce();
        }

        // Destroy after max lifetime to prevent objects from flying forever
        if (maxLifetime > 0)
        {
            Destroy(gameObject, maxLifetime);
        }

        // If we have a DamagingRock component, make sure it's configured correctly
        if (damagingRock != null)
        {
            // We'll handle destruction in this component, so disable auto-destruction in DamagingRock
            damagingRock.destroyOnAnyImpact = false;

            // Log that we're working with a DamagingRock
            Debug.Log("FlyToPlayer is working with a DamagingRock component");
        }

        // Set up physics to ignore all collisions except with player
        SetupCollisionIgnore();
    }

    private void SetupCollisionIgnore()
    {
        if (projectileCollider == null)
        {
            Debug.LogWarning("FlyToPlayer: No Collider found on this object!");
            return;
        }

        bool hasPlayerTransform = TryResolvePlayerTransform(out Transform resolvedPlayerTransform);
        Collider[] allColliders = Object.FindObjectsOfType<Collider>();

        foreach (Collider otherCollider in allColliders)
        {
            if (otherCollider == null || otherCollider == projectileCollider || otherCollider.isTrigger)
            {
                continue;
            }

            if (IsPlayerCollider(otherCollider, resolvedPlayerTransform, hasPlayerTransform))
            {
                continue;
            }

            Physics.IgnoreCollision(projectileCollider, otherCollider, true);
        }

        Debug.Log("FlyToPlayer: Set up to ignore all collisions except with player-tagged objects");
    }

    private bool TryResolvePlayerTransform(out Transform resolvedPlayerTransform)
    {
        if (cachedPlayerTransform != null)
        {
            resolvedPlayerTransform = cachedPlayerTransform;
            return true;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject == null)
        {
            resolvedPlayerTransform = null;
            return false;
        }

        cachedPlayerTransform = playerObject.transform;
        resolvedPlayerTransform = cachedPlayerTransform;
        return true;
    }

    private bool IsPlayerCollider(Collider candidateCollider, Transform resolvedPlayerTransform, bool hasPlayerTransform)
    {
        if (candidateCollider.CompareTag(playerTag))
        {
            return true;
        }

        if (!hasPlayerTransform || resolvedPlayerTransform == null)
        {
            return false;
        }

        return candidateCollider.transform.IsChildOf(resolvedPlayerTransform);
    }

    private bool IsPlayerObject(GameObject candidateObject)
    {
        if (candidateObject == null)
        {
            return false;
        }

        if (candidateObject.CompareTag(playerTag))
        {
            return true;
        }

        if (!TryResolvePlayerTransform(out Transform resolvedPlayerTransform) || resolvedPlayerTransform == null)
        {
            return false;
        }

        return candidateObject.transform.IsChildOf(resolvedPlayerTransform);
    }

    public void FindPlayerOnce()
    {
        if (!TryResolvePlayerTransform(out Transform resolvedPlayerTransform) || resolvedPlayerTransform == null)
        {
            Debug.LogWarning($"FlyToPlayer: No object with tag '{playerTag}' found!");
            return;
        }

        // Record the player's position at this moment only
        targetPosition = resolvedPlayerTransform.position;

        // Calculate the direction to fly (only once)
        flyDirection = (targetPosition - transform.position).normalized;

        isInitialized = true;
        StartCoroutine(AccelerateTowardsTarget());

        Debug.Log($"FlyToPlayer targeting player at position {targetPosition}");
    }

    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
        flyDirection = (targetPosition - transform.position).normalized;
        isInitialized = true;
        StartCoroutine(AccelerateTowardsTarget());

        Debug.Log($"FlyToPlayer targeting custom position {targetPosition}");
    }

    private IEnumerator AccelerateTowardsTarget()
    {
        accelerationProgress = 0f;

        while (accelerationProgress < 1f)
        {
            accelerationProgress += Time.deltaTime / accelerationTime;
            yield return null;
        }

        accelerationProgress = 1f;
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
            return;

        // Calculate current speed with acceleration curve
        currentSpeed = moveSpeed * accelerationCurve.Evaluate(accelerationProgress);

        // Move in the fixed direction (does not update with player movement)
        rb.linearVelocity = flyDirection * currentSpeed;

        // Keep rotation pointing in the travel direction
        if (rotationSpeed > 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flyDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Only respond to collisions with the player
        if (IsPlayerObject(collision.gameObject))
        {
            HandlePlayerCollision(collision.gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only respond to triggers with the player
        if (IsPlayerObject(other.gameObject))
        {
            HandlePlayerCollision(other.gameObject);
        }
    }

    private void HandlePlayerCollision(GameObject player)
    {
        Debug.Log($"FlyToPlayer hit player: {player.name}");

        // Spawn hit effect if one is specified
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        // Destroy if configured to do so
        if (destroyOnPlayerCollision)
        {
            // Let DamagingRock handle the damage application if it exists
            // Otherwise destroy this object directly
            if (damagingRock == null)
            {
                Destroy(gameObject);
            }
        }
    }
}