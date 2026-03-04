using System.Collections;
using UnityEngine;

public class JumpAttack : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpHeight = 10f;
    [SerializeField] private float jumpDuration = 1.0f;
    [SerializeField] private float hangTime = 0.5f;
    [SerializeField] private float fallDuration = 0.8f;
    [SerializeField] private AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Cooldown Settings")]
    [SerializeField] private float attackCooldown = 5.0f;
    private float cooldownTimer = 0f;

    [Header("Shockwave Settings")]
    [SerializeField] private GameObject shockwavePrefab;
    [SerializeField] private float shockwaveDuration = 0.5f;

    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeDuration = 0.7f;
    [SerializeField] private float shakeMagnitude = 0.3f;
    [SerializeField] private bool useDistanceBasedShake = true;
    [SerializeField] private float maxShakeDistance = 30f;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip rumbleSound;
    [SerializeField] private float impactVolume = 1.0f;
    [SerializeField] private float rumbleVolume = 0.7f;
    [SerializeField] private float rumbleDuration = 2.0f;

    private bool isJumping = false;
    private float groundLevel;
    private CameraController mainCamera;
    private AudioSource audioSource;
    private AudioSource rumbleAudioSource;

    private void Start()
    {
        // Store the initial Y position as the ground level
        groundLevel = transform.position.y;

        // Find the camera controller reference
        if (Camera.main != null)
        {
            mainCamera = Camera.main.GetComponent<CameraController>();
            if (mainCamera == null)
            {
                Debug.LogWarning("CameraController component not found on main camera!");
            }
        }
        else
        {
            Debug.LogWarning("Main camera not found in scene!");
        }

        // Set up audio sources
        SetupAudioSources();

        StartJumpAttack();
    }

    private void SetupAudioSources()
    {
        // Get or add the main audio source for impact sound
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 5.0f;
            audioSource.maxDistance = 50.0f;
        }

        // Create a separate audio source for the rumble sound
        GameObject rumbleAudioObj = new GameObject("RumbleAudio");
        rumbleAudioObj.transform.parent = transform;
        rumbleAudioObj.transform.localPosition = Vector3.zero;
        rumbleAudioSource = rumbleAudioObj.AddComponent<AudioSource>();
        rumbleAudioSource.spatialBlend = 1.0f; // 3D sound
        rumbleAudioSource.rolloffMode = AudioRolloffMode.Linear;
        rumbleAudioSource.minDistance = 10.0f;
        rumbleAudioSource.maxDistance = 100.0f;
        rumbleAudioSource.loop = true; // Allow looping for the rumble
    }

    private void Update()
    {
        // Decrease cooldown timer
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }
        if (transform.position.y != groundLevel)
        {
            return;
        }

        // If not jumping and cooldown is finished, the boss can jump again
        if (!isJumping && cooldownTimer <= 0)
        {
            // You can trigger the jump based on player distance, 
            // randomly, or other game logic here


            StartJumpAttack();
            // For AI-driven behavior, you might want to do something like:
            // if (ShouldJumpBasedOnAI())
            // {
            //     StartJumpAttack();
            // }
        }
    }

    private void StartJumpAttack()
    {
        if (!isJumping && cooldownTimer <= 0)
        {
            isJumping = true;
            StartCoroutine(JumpAndSlamCoroutine());
        }
    }

    private IEnumerator JumpAndSlamCoroutine()
    {
        // Jump phase - moving upward
        float startTime = Time.time;
        Vector3 startPos = transform.position;
        Vector3 jumpTargetPos = startPos + new Vector3(0, jumpHeight, 0);

        while (Time.time < startTime + jumpDuration)
        {
            float t = (Time.time - startTime) / jumpDuration;
            // Use easeOut curve for more natural jump
            float smoothT = 1 - (1 - t) * (1 - t);
            transform.position = Vector3.Lerp(startPos, jumpTargetPos, smoothT);
            yield return null;
        }

        // Ensure we reach exact target position
        transform.position = jumpTargetPos;

        // Hang time at apex
        yield return new WaitForSeconds(hangTime);

        // Fall phase
        startTime = Time.time;
        startPos = transform.position;
        Vector3 landingPos = new Vector3(startPos.x, groundLevel, startPos.z);

        while (Time.time < startTime + fallDuration)
        {
            float t = (Time.time - startTime) / fallDuration;

            // Different acceleration curve options for descent:

            // Option 1: Quadratic (t) - moderately accelerating fall (current)
            // float smoothT = t * t;

            // Option 2: Cubic (t) - more aggressive acceleration
            // float smoothT = t * t * t;

            // Option 3: Quartic (t) - very aggressive acceleration
            float smoothT = fallCurve.Evaluate(t);

            // Option 4: Custom curve - adjust values for different feel
            // float gravity = 2.5f;
            // float smoothT = Mathf.Pow(t, gravity);

            transform.position = Vector3.Lerp(startPos, landingPos, smoothT);
            yield return null;
        }

        // Ensure landing position is exact
        transform.position = landingPos;

        // Create shockwave on impact
        CreateShockwave();

        // Trigger camera shake on impact
        TriggerCameraShake();

        // Reset cooldown
        cooldownTimer = attackCooldown;
        isJumping = false;
    }

    private void CreateShockwave()
    {
        // This will be implemented later as requested
        // The code below is just a placeholder

        if (shockwavePrefab != null)
        {
            GameObject shockwave = Instantiate(shockwavePrefab, transform.position, Quaternion.identity);
            DamagingRock damagingRock = shockwave.GetComponent<DamagingRock>();
            if (damagingRock != null)
            {
                damagingRock.canDamagePlayer = true;
                damagingRock.canDamageBoss = false;
                damagingRock.SetOwner(transform);
            }
            Destroy(shockwave, shockwaveDuration);
        }
        else
        {
            Debug.LogWarning("Shockwave prefab not assigned to BossJumpSlam script!");
        }
    }

    private void TriggerCameraShake()
    {
        // Calculate distance factor for both shake and sound effects
        float distanceFactor = 1.0f;
        if (useDistanceBasedShake && Camera.main != null)
        {
            float distanceToCamera = Vector3.Distance(transform.position, Camera.main.transform.position);
            distanceFactor = Mathf.Clamp01(1 - (distanceToCamera / maxShakeDistance));
        }

        // Trigger camera shake
        if (mainCamera != null)
        {
            // Scale the magnitude by distance (closer = stronger shake)
            float adjustedMagnitude = shakeMagnitude * distanceFactor;

            // Only shake if we're close enough to have a visible effect
            if (distanceFactor > 0.05f)
            {
                mainCamera.ShakeCamera(shakeDuration, adjustedMagnitude);
            }
        }

        // Play impact sound
        PlayImpactSound(distanceFactor);
    }

    private void PlayImpactSound(float distanceFactor)
    {
        // Play the initial impact sound
        if (impactSound != null && audioSource != null)
        {
            audioSource.clip = impactSound;
            audioSource.volume = impactVolume;
            audioSource.pitch = Random.Range(0.9f, 1.1f); // Slight pitch variation
            audioSource.Play();
        }

        // Play the rumble sound
        if (rumbleSound != null && rumbleAudioSource != null)
        {
            StartCoroutine(PlayRumbleSound(distanceFactor));
        }
    }

    private IEnumerator PlayRumbleSound(float distanceFactor)
    {
        // Set up the rumble sound
        rumbleAudioSource.clip = rumbleSound;
        rumbleAudioSource.volume = 0; // Start at zero volume
        rumbleAudioSource.pitch = Random.Range(0.8f, 1.0f); // Lower pitch for rumble
        rumbleAudioSource.Play();

        // Fade in the rumble
        float startTime = Time.time;
        float fadeInDuration = 0.2f;
        while (Time.time < startTime + fadeInDuration)
        {
            float t = (Time.time - startTime) / fadeInDuration;
            rumbleAudioSource.volume = Mathf.Lerp(0, rumbleVolume * distanceFactor, t);
            yield return null;
        }

        // Hold at max volume
        rumbleAudioSource.volume = rumbleVolume * distanceFactor;

        // Wait for a while
        yield return new WaitForSeconds(rumbleDuration - fadeInDuration - 0.5f);

        // Fade out the rumble
        startTime = Time.time;
        float fadeOutDuration = 0.5f;
        float initialVolume = rumbleAudioSource.volume;
        while (Time.time < startTime + fadeOutDuration)
        {
            float t = (Time.time - startTime) / fadeOutDuration;
            rumbleAudioSource.volume = Mathf.Lerp(initialVolume, 0, t);
            yield return null;
        }

        // Ensure it stops completely
        rumbleAudioSource.Stop();
        rumbleAudioSource.volume = 0;
    }

    // Method that can be called by external scripts to trigger the jump
    public void TriggerJumpAttack()
    {
        StartJumpAttack();
    }

    // Helper method that returns true if boss is currently performing jump attack
    public bool IsPerformingJumpAttack()
    {
        return isJumping;
    }
}