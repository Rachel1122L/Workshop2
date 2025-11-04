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

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;
    private Vector3 lastValidPosition;

    // ADDED: PhotonView reference
    private PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f);
        lastValidPosition = transform.position;

        // Disable camera and audio for non-owned boats
        if (!photonView.IsMine)
        {
            // Disable all cameras on this boat
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }

            // Disable audio listeners
            AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }

            // OPTIONAL: You can choose to disable this script or keep it for visual updates
            // enabled = false; // Uncomment if you want to completely disable non-local boats
        }
    }

    void Update()
    {
        // ADDED: Only process input for local player
        if (!photonView.IsMine)
            return;

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");

        HandleMovement();
        HandleTurning();
        ApplyFakeBuoyancy();
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

        Vector3 potentialPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;

        float terrainY = GetTerrainHeightAt(potentialPosition);
        float waterY = waterHeight;

        if (terrainY > waterY + collisionBuffer)
        {
            currentSpeed = 0f;
            transform.position = lastValidPosition;
        }
        else
        {
            transform.position = potentialPosition;
            lastValidPosition = transform.position;
        }

        transform.position = new Vector3(transform.position.x, waterY, transform.position.z);
    }

    void HandleTurning()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }

    void ApplyFakeBuoyancy()
    {
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;

        float pitch = -Mathf.Lerp(0, tiltAmount, Mathf.Abs(currentSpeed) / maxSpeed) * Mathf.Sign(currentSpeed);

        float roll = -turnInput * rollAmount;

        Quaternion targetRotation = Quaternion.Euler(baseRotation.eulerAngles.x + pitch, transform.eulerAngles.y, baseRotation.eulerAngles.z + roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * buoyancySmooth);

        Vector3 pos = transform.position;
        pos.y = waterHeight + bob;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * buoyancySmooth);
    }

    float GetTerrainHeightAt(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * terrainCheckHeight, Vector3.down, out hit, terrainCheckHeight * 2f, terrainLayer))
        {
            return hit.point.y;
        }
        return Mathf.NegativeInfinity;
    }

    // ADDED: Public property to check if this is local player's boat
    public bool IsLocalPlayer
    {
        get { return photonView != null && photonView.IsMine; }
    }
}

// Original Code//
/*using UnityEngine;
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

    private float currentSpeed = 0f;
    private float moveInput = 0f;
    private float turnInput = 0f;
    private float bobOffset;
    private Quaternion baseRotation;
    private Vector3 lastValidPosition;

    PhotonView photonView;

    void Start()
    {
        photonView = GetComponent<PhotonView>();

        baseRotation = transform.rotation;
        bobOffset = Random.Range(0f, 100f);
        lastValidPosition = transform.position;

        // Disable camera and audio for non-owned boats
        if (!photonView.IsMine)
        {
            // Disable all cameras on this boat
            Camera[] cameras = GetComponentsInChildren<Camera>();
            foreach (Camera cam in cameras)
            {
                cam.enabled = false;
            }

            // Disable audio listeners
            AudioListener[] listeners = GetComponentsInChildren<AudioListener>();
            foreach (AudioListener listener in listeners)
            {
                listener.enabled = false;
            }

            // Optionally disable this script to prevent unnecessary updates
            enabled = false;
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxisRaw("Horizontal");

        HandleMovement();
        HandleTurning();
        ApplyFakeBuoyancy();
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

        Vector3 potentialPosition = transform.position + transform.forward * currentSpeed * Time.deltaTime;

        float terrainY = GetTerrainHeightAt(potentialPosition);
        float waterY = waterHeight;

        if (terrainY > waterY + collisionBuffer)
        {
            currentSpeed = 0f;
            transform.position = lastValidPosition;
        }
        else
        {
            transform.position = potentialPosition;
            lastValidPosition = transform.position;
        }

        transform.position = new Vector3(transform.position.x, waterY, transform.position.z);
    }

    void HandleTurning()
    {
        if (Mathf.Abs(turnInput) > 0.1f)
        {
            float speedFactor = Mathf.Clamp01(Mathf.Abs(currentSpeed) / maxSpeed);
            float rotationAmount = turnInput * turnSpeed * speedFactor * turnLimit * Time.deltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }
    }

    void ApplyFakeBuoyancy()
    {
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;

        float pitch = -Mathf.Lerp(0, tiltAmount, Mathf.Abs(currentSpeed) / maxSpeed) * Mathf.Sign(currentSpeed);

        float roll = -turnInput * rollAmount;

        Quaternion targetRotation = Quaternion.Euler(baseRotation.eulerAngles.x + pitch, transform.eulerAngles.y, baseRotation.eulerAngles.z + roll);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * buoyancySmooth);

        Vector3 pos = transform.position;
        pos.y = waterHeight + bob;
        transform.position = Vector3.Lerp(transform.position, pos, Time.deltaTime * buoyancySmooth);
    }

    float GetTerrainHeightAt(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up * terrainCheckHeight, Vector3.down, out hit, terrainCheckHeight * 2f, terrainLayer))
        {
            return hit.point.y;
        }
        return Mathf.NegativeInfinity;
    }
}*/