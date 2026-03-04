using System.Collections;
using UnityEngine;

public class SpawnAndFlyTowardPlayer : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The prefab to spawn")]
    public GameObject objectToSpawn;

    [Tooltip("The object to spawn from (leave empty to use this GameObject)")]
    public Transform spawnPoint;

    [Tooltip("The player's transform that objects will target")]
    public Transform playerTransform;

    [Header("Movement Settings")]
    [Tooltip("Speed at which objects move toward the player")]
    public float flySpeed = 5f;

    [Tooltip("Whether to use homing behavior (continuously update direction)")]
    public bool homingBehavior = false;

    [Tooltip("Random offset range for spawn position")]
    public Vector3 spawnPositionRandomOffset = new Vector3(1f, 1f, 1f);

    [Header("Timing Settings")]
    [Tooltip("Time between spawns in seconds")]
    public float spawnInterval = 2f;

    [Tooltip("Whether to spawn continuously or just once")]
    public bool continuousSpawning = true;

    private static Transform cachedPlayerTransform;
    private void Start()
    {
        // Validate parameters
        if (objectToSpawn == null)
        {
            Debug.LogWarning("No spawn object assigned to ObjectSpawnerTargetingPlayer! Trying to find a rock prefab...");
            
            // Tìm kiếm prefab đá trong Resources hoặc sử dụng một prefab mặc định
            objectToSpawn = Resources.Load<GameObject>("Rock_1");
            
            // Nếu vẫn không tìm thấy, tạo một đối tượng đơn giản
            if (objectToSpawn == null)
            {
                // Tạo một đối tượng đơn giản để thay thế
                GameObject tempObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tempObject.name = "DefaultRock";
                tempObject.AddComponent<Rigidbody>();
                tempObject.AddComponent<DamagingRock>();
                
                // Lưu đối tượng này và sử dụng nó làm prefab
                DontDestroyOnLoad(tempObject);
                tempObject.SetActive(false);
                objectToSpawn = tempObject;
                
                Debug.Log("Created a default rock object as fallback");
            }
            else
            {
                Debug.Log("Found rock prefab in Resources");
            }
        }

        if (playerTransform == null && !TryResolvePlayerTransform(out playerTransform))
        {
            Debug.LogError("No player transform assigned and no GameObject with tag 'Player' found!");
            return;
        }

        if (playerTransform != null)
        {
            cachedPlayerTransform = playerTransform;
        }

        // If no spawn point assigned, use this object's transform
        if (spawnPoint == null)
        {
            spawnPoint = this.transform;
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

    // Spawn a single object at the spawn point's position
    private void SpawnObject()
    {
        if (objectToSpawn == null)
        {
            Debug.LogError("Cannot spawn: objectToSpawn is null!");
            return;
        }
        
        if (playerTransform == null && !TryResolvePlayerTransform(out playerTransform))
        {
            Debug.LogWarning("No player transform to target! Skipping spawn.");
            return;
        }

        // Calculate random offset for spawn position
        Vector3 randomOffset = new Vector3(
            Random.Range(-spawnPositionRandomOffset.x, spawnPositionRandomOffset.x),
            Random.Range(-spawnPositionRandomOffset.y, spawnPositionRandomOffset.y),
            Random.Range(-spawnPositionRandomOffset.z, spawnPositionRandomOffset.z)
        );

        // Create the spawn position with random offset
        Vector3 spawnPosition = spawnPoint.position + randomOffset;

        // Calculate initial direction to player
        Vector3 directionToPlayer = (playerTransform.position - spawnPosition).normalized;

        try
        {
            // Instantiate the object at the spawn position
            GameObject newObject = Instantiate(objectToSpawn, spawnPosition, Quaternion.LookRotation(directionToPlayer));
            
            // Log để debug
            Debug.Log($"Spawned rock at position {spawnPosition}");
            
            // Đảm bảo đối tượng có Rigidbody và được cấu hình đúng
            Rigidbody rb = newObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = newObject.AddComponent<Rigidbody>();
                Debug.Log("Added Rigidbody to spawned object");
            }
            
            // Cấu hình Rigidbody để bay đến người chơi
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            Debug.Log("Configured Rigidbody: gravity disabled, rotation frozen");

            // Add the flying behavior component to the spawned object
            FlyTowardsPlayer flyScript = newObject.GetComponent<FlyTowardsPlayer>();
            if (flyScript == null)
            {
                flyScript = newObject.AddComponent<FlyTowardsPlayer>();
                Debug.Log("Added FlyTowardsPlayer component to spawned object");
            }
            
            // Cấu hình FlyTowardsPlayer
            flyScript.playerTransform = playerTransform;
            flyScript.flySpeed = flySpeed;
            flyScript.homingBehavior = homingBehavior;
            Debug.Log($"Configured FlyTowardsPlayer: speed={flySpeed}, homing={homingBehavior}");
            
            // Đảm bảo đối tượng có DamagingRock để gây sát thương
            DamagingRock damagingRock = newObject.GetComponent<DamagingRock>();
            if (damagingRock == null)
            {
                damagingRock = newObject.AddComponent<DamagingRock>();
                damagingRock.damage = 1f;
                Debug.Log("Added DamagingRock component to spawned object");
            }
            
            // Cấu hình DamagingRock
            damagingRock.destroyOnAnyImpact = true;
            damagingRock.canDamagePlayer = true;
            damagingRock.canDamageBoss = false;
            damagingRock.SetOwner(transform);
            Debug.Log("Configured DamagingRock for boss projectile: owner set, boss damage disabled");
            // Xóa bất kỳ component FlyToPlayer nào để tránh xung đột
            FlyToPlayer oldFlyComponent = newObject.GetComponent<FlyToPlayer>();
            if (oldFlyComponent != null)
            {
                Destroy(oldFlyComponent);
                Debug.Log("Removed conflicting FlyToPlayer component");
            }
            
            Debug.Log($"Rock is now flying towards player at {playerTransform.position}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error spawning object: {e.Message}");
        }
    }

    private bool TryResolvePlayerTransform(out Transform resolvedPlayerTransform)
    {
        if (playerTransform != null)
        {
            cachedPlayerTransform = playerTransform;
            resolvedPlayerTransform = playerTransform;
            return true;
        }

        if (cachedPlayerTransform != null)
        {
            resolvedPlayerTransform = cachedPlayerTransform;
            return true;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            resolvedPlayerTransform = null;
            return false;
        }

        cachedPlayerTransform = player.transform;
        resolvedPlayerTransform = cachedPlayerTransform;
        Debug.Log("Player found automatically. Using " + player.name + " as target.");
        return true;
    }
    // Public method to spawn an object (can be called from other scripts or events)
    public void SpawnNow()
    {
        SpawnObject();
    }
}