using UnityEngine;
using Photon.Pun;

public class SimpleBoatController : MonoBehaviourPunCallbacks
{
    [Header("Movement Settings")]
    public float acceleration = 5f;
    public float maxSpeed = 10f;
    public float reverseSpeed = 4f;
    public float deceleration = 3f;

    [Header("Turning Settings")]
    public float turnSpeed = 45f;
    public float turnLimit = 1f;

    [Header("Fake Buoyancy Settings")]
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.15f;
    public float tiltAmount = 5f;
    public float rollAmount = 4f;
    public float buoyancySmooth = 2f;

    [Header("Environment Settings")]
    public float waterHeight = 0f;
    public LayerMask terrainLayer;
    public float terrainCheckHeight = 100f;
    public float collisionBuffer = 0.5f;
    public float stopHeight = 3f; // Height at which boat should stop

    [Header("Auto-Correction Settings")]
    public bool enableAutoCorrection = true;
    public float correctionCheckInterval = 0.5f;
    public float maxPositionOffset = 2f;
    public float maxRotationOffset = 15f;
    public float correctionSpeed = 2f;
    public float emergencyCorrectionSpeed = 5f;
    public float stuckTimeThreshold = 3f;

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;
    private Vector3 lastValidPosition;
    private Quaternion lastValidRotation;

    // Auto-correction variables
    private Vector3 expectedPosition;
    private Quaternion expectedRotation;
    private float lastCorrectionCheck;
    private bool isCorrecting = false;
    private float stuckTimer = 0f;
    private bool wasMoving = false;
    private Vector3 initialPosition;
    private new PhotonView photonView;

    void Start()
    {
        if (!base.photonView.IsMine)
        {
            // Disable cameras and audio listeners
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }

            AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }

            enabled = false;
            return;
        }

        // Store the original position BEFORE any systems interfere
        initialPosition = transform.position;

        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f);

        // Use the original position, don't let systems override it initially
        lastValidPosition = initialPosition;
        expectedPosition = initialPosition;
        expectedRotation = transform.rotation;

        // Initialize last correction check
        lastCorrectionCheck = Time.time;
    }

    void Update()
    {
        if (!base.photonView.IsMine)
            return;

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");

        HandleMovement();
        HandleTurning();
        ApplyFakeBuoyancy();

        if (enableAutoCorrection)
        {
            CheckForCorrection();
            ApplyCorrection();
        }
    }

    void HandleMovement()
    {
        if (moveInput != 0)
        {
            float targetSpeed = moveInput > 0 ? maxSpeed : -reverseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0, deceleration * Time.deltaTime);
        }

        // Calculate expected movement
        Vector3 movement = transform.forward * currentSpeed * Time.deltaTime;
        expectedPosition = transform.position + movement;

        // Update expected rotation based on turning
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            expectedRotation = transform.rotation * Quaternion.Euler(0, rotationAmount, 0);
        }
        else
        {
            expectedRotation = transform.rotation;
        }

        // Check for terrain collision - FIXED VERSION
        Vector3 potentialPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;

        // Check terrain height at the potential position
        float terrainY = GetTerrainHeightAt(potentialPosition);

        // Debug information to see what's happening
        //Debug.Log($"Boat Y: {transform.position.y}, Terrain Y: {terrainY}, Stop Height: {stopHeight}");

        // Stop if terrain is above our stop height threshold
        if (terrainY > stopHeight)
        {
            currentSpeed = 0f;
            transform.position = lastValidPosition;
            expectedPosition = lastValidPosition;
            Debug.Log("TERRAIN COLLISION: Movement stopped due to high terrain");
        }
        else
        {
            transform.position = potentialPosition;
            lastValidPosition = transform.position;
        }

        // REMOVED the line that forces Y position to waterHeight
        // This was causing conflicts with the buoyancy system
        // transform.position = new Vector3(transform.position.x, waterY, transform.position.z);
    }

    void HandleTurning()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
            lastValidRotation = transform.rotation;
        }
    }

    void ApplyFakeBuoyancy()
    {
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;

        float pitch = -Mathf.Lerp(0, tiltAmount, Mathf.Abs(currentSpeed) / maxSpeed) * Mathf.Sign(currentSpeed);
        float roll = -turnInput * rollAmount;

        Quaternion targetRotation = Quaternion.Euler(baseRotation.eulerAngles.x + pitch, transform.eulerAngles.y, baseRotation.eulerAngles.z + roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * buoyancySmooth);

        // Calculate target Y position - only apply buoyancy if we're in water
        Vector3 pos = transform.position;
        float targetY = pos.y; // Start with current Y

        // Only apply buoyancy if we're at or near water level
        if (pos.y <= waterHeight + 1f) // Within 1 unit of water surface
        {
            targetY = waterHeight + bob;
        }

        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * buoyancySmooth);
        transform.position = pos;
    }

    void CheckForCorrection()
    {
        // Only check periodically to save performance
        if (Time.time - lastCorrectionCheck < correctionCheckInterval)
            return;

        lastCorrectionCheck = Time.time;

        // Calculate position and rotation offsets
        float positionOffset = Vector3.Distance(transform.position, expectedPosition);
        float rotationOffset = Quaternion.Angle(transform.rotation, expectedRotation);

        // Check if we're stuck (not moving but should be)
        bool isMoving = Mathf.Abs(currentSpeed) > 0.1f || Mathf.Abs(turnInput) > 0.1f;

        if (isMoving && !wasMoving)
        {
            // Just started moving, reset stuck timer
            stuckTimer = 0f;
        }
        else if (!isMoving && wasMoving)
        {
            // Just stopped moving
            stuckTimer = 0f;
        }

        wasMoving = isMoving;

        // If we should be moving but position isn't changing much, we might be stuck
        if (isMoving && positionOffset < 0.1f)
        {
            stuckTimer += correctionCheckInterval;
        }
        else
        {
            stuckTimer = Mathf.Max(0, stuckTimer - correctionCheckInterval);
        }

        // Determine if correction is needed
        bool needsCorrection = positionOffset > maxPositionOffset ||
                              rotationOffset > maxRotationOffset ||
                              stuckTimer > stuckTimeThreshold;

        if (needsCorrection && !isCorrecting)
        {
            isCorrecting = true;
            Debug.Log("Auto-correction activated. Position offset: " + positionOffset +
                     ", Rotation offset: " + rotationOffset +
                     ", Stuck time: " + stuckTimer);
        }
        else if (!needsCorrection && isCorrecting)
        {
            isCorrecting = false;
        }
    }

    void ApplyCorrection()
    {
        if (!isCorrecting)
            return;

        // Calculate correction speed (faster if stuck for longer)
        float actualCorrectionSpeed = stuckTimer > stuckTimeThreshold ?
                                     emergencyCorrectionSpeed : correctionSpeed;

        // Smoothly correct position
        transform.position = Vector3.Lerp(transform.position, expectedPosition,
                                         actualCorrectionSpeed * Time.deltaTime);

        // Smoothly correct rotation (only yaw for direction, keep buoyancy effects)
        Vector3 targetEuler = expectedRotation.eulerAngles;
        Vector3 currentEuler = transform.rotation.eulerAngles;

        // Only correct yaw (y-axis rotation) to maintain buoyancy pitch/roll
        float correctedYaw = Mathf.LerpAngle(currentEuler.y, targetEuler.y,
                                           actualCorrectionSpeed * Time.deltaTime);

        transform.rotation = Quaternion.Euler(currentEuler.x, correctedYaw, currentEuler.z);

        // If correction is nearly complete, stop correcting
        float positionError = Vector3.Distance(transform.position, expectedPosition);
        float rotationError = Mathf.Abs(Mathf.DeltaAngle(transform.eulerAngles.y, expectedRotation.eulerAngles.y));

        if (positionError < 0.1f && rotationError < 1f)
        {
            isCorrecting = false;
            stuckTimer = 0f;
        }
    }

    float GetTerrainHeightAt(Vector3 position)
    {
        RaycastHit hit;
        Vector3 rayStart = position + Vector3.up * terrainCheckHeight;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, terrainCheckHeight * 2f, terrainLayer))
        {
            Debug.DrawLine(rayStart, hit.point, Color.red, 1f); // Visual debug
            return hit.point.y;
        }

        Debug.DrawRay(rayStart, Vector3.down * terrainCheckHeight * 2f, Color.green, 1f); // Visual debug
        return Mathf.NegativeInfinity;
    }

    // Optional: Force correction when collision detected
    void OnCollisionEnter(Collision collision)
    {
        if (!base.photonView.IsMine || !enableAutoCorrection)
            return;

        // If we hit something other than terrain/water, trigger correction
        if (((1 << collision.gameObject.layer) & terrainLayer) == 0)
        {
            isCorrecting = true;
            stuckTimer = stuckTimeThreshold; // Force emergency correction
            Debug.Log("Collision detected, forcing auto-correction");
        }
    }

    // Optional: Visual debug in scene view
    void OnDrawGizmosSelected()
    {
        if (!base.photonView.IsMine || !enableAutoCorrection)
            return;

        // Draw expected position
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(expectedPosition, 0.5f);

        // Draw line from current to expected position
        Gizmos.color = isCorrecting ? Color.red : Color.yellow;
        Gizmos.DrawLine(transform.position, expectedPosition);

        // Draw expected forward direction
        Gizmos.color = Color.blue;
        Vector3 expectedForward = expectedRotation * Vector3.forward * 2f;
        Gizmos.DrawLine(expectedPosition, expectedPosition + expectedForward);

        // Draw stop height plane
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(new Vector3(transform.position.x, stopHeight, transform.position.z),
                           new Vector3(10f, 0.1f, 10f));
    }
}