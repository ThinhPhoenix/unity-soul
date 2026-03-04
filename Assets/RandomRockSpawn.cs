using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The prefab to spawn")]
    public GameObject objectToSpawn;

    [Header("Position Settings")]
    [Tooltip("Minimum X position for spawning")]
    public float minX = -10f;

    [Tooltip("Maximum X position for spawning")]
    public float maxX = 10f;

    [Tooltip("Y position for spawning (height)")]
    public float spawnHeight = 5f;

    [Tooltip("Minimum Z position for spawning")]
    public float minZ = -10f;

    [Tooltip("Maximum Z position for spawning")]
    public float maxZ = 10f;

    [Header("Behavior Settings")]
    [Tooltip("Should rocks fly towards the player? If false, they will fall with gravity")]
    public bool rocksFlyToPlayer = false;

    [Header("Timing Settings")]
    [Tooltip("Time between spawns in seconds")]
    public float spawnInterval = 2f;

    [Tooltip("Whether to spawn continuously or just once")]
    public bool continuousSpawning = true;

    private void Start()
    {
        // Validate parameters
        if (objectToSpawn == null)
        {
            Debug.LogError("No spawn object assigned to RandomSpawner!");
            return;
        }

        // Make sure the object to spawn has a Rigidbody
        if (objectToSpawn.GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning("The object to spawn doesn't have a Rigidbody component. It won't fall unless affected by gravity.");
        }

        if (continuousSpawning)
        {
            // Start the continuous spawning coroutine
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            // Just spawn once
            SpawnObject();
        }
    }

    // Coroutine for continuous spawning
    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // Spawn a single object at a random position
    private void SpawnObject()
    {
        // Calculate a random position within the specified range
        float randomX = Random.Range(minX, maxX);
        float randomZ = Random.Range(minZ, maxZ);
        Vector3 spawnPosition = new Vector3(randomX, spawnHeight, randomZ);

        // Spawn the object
        GameObject spawnedObject = Instantiate(objectToSpawn, spawnPosition, Quaternion.identity);
        
        // Kiểm tra xem có nên thêm FlyToPlayer hay không
        bool shouldFlyToPlayer = rocksFlyToPlayer; // Sử dụng biến rocksFlyToPlayer để quyết định
        
        // Lấy Rigidbody của đá
        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (shouldFlyToPlayer)
            {
                // Nếu đá bay đến người chơi, tắt trọng lực và đặt constraints
                rb.useGravity = false;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                Debug.Log("Rock will fly towards player - Gravity disabled");
            }
            else
            {
                // Nếu đá rơi xuống, bật trọng lực và bỏ constraints
                rb.useGravity = true;
                rb.constraints = RigidbodyConstraints.None;
                Debug.Log("Rock will fall with gravity - Gravity enabled");
            }
        }

        // Thêm hoặc xóa component FlyToPlayer tùy thuộc vào cấu hình
        if (shouldFlyToPlayer)
        {
            // Thêm component FlyToPlayer nếu chưa có
            FlyToPlayer flyComponent = spawnedObject.GetComponent<FlyToPlayer>();
            if (flyComponent == null)
            {
                flyComponent = spawnedObject.AddComponent<FlyToPlayer>();
                // Cấu hình các thuộc tính của FlyToPlayer
                flyComponent.moveSpeed = 5f;
                flyComponent.rotationSpeed = 3f;
                flyComponent.accelerationTime = 1f;
                flyComponent.findPlayerOnStart = true;
                flyComponent.playerTag = "Player";
                flyComponent.maxLifetime = 15f;
                flyComponent.destroyOnPlayerCollision = true;
                Debug.Log("Added FlyToPlayer component to rock with default settings");
            }
            else
            {
                // Đảm bảo FlyToPlayer được cấu hình đúng
                flyComponent.findPlayerOnStart = true;
                Debug.Log("Rock already has FlyToPlayer component - Ensured it's configured correctly");
            }
        }
        else
        {
            // Xóa component FlyToPlayer nếu có
            FlyToPlayer flyComponent = spawnedObject.GetComponent<FlyToPlayer>();
            if (flyComponent != null)
            {
                Destroy(flyComponent);
                Debug.Log("Removed FlyToPlayer component from rock");
            }
        }

        DamagingRock damagingRock = spawnedObject.GetComponent<DamagingRock>();
        if (damagingRock == null)
        {
            damagingRock = spawnedObject.AddComponent<DamagingRock>();
            Debug.Log("Added DamagingRock component to rock");
        }

        if (shouldFlyToPlayer)
        {
            damagingRock.canDamagePlayer = true;
            damagingRock.canDamageBoss = false;
            damagingRock.SetOwner(transform);
        }

        // Log thông tin về đá đã spawn
        Debug.Log($"Spawned rock at position {spawnPosition}. Flying to player: {shouldFlyToPlayer}");
    }

    // Public method to spawn an object (can be called from other scripts or events)
    public void SpawnNow()
    {
        SpawnObject();
    }
}