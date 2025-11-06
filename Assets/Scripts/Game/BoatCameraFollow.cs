using UnityEngine;

public class BoatCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public float waterLevel = 0f;

    [Header("Camera Settings")]
    public float distance = 12f;
    public float height = 6f;
    public float sensitivity = 2f;
    public float smoothness = 0.1f;

    [Header("DREDGE-style Settings")]
    public float minHeightAboveWater = 1f;
    public float pitchAngle = 25f; // Fixed downward viewing angle
    public float orbitSpeedMultiplier = 1.5f;
    public float autoRealignSpeed = 1.5f; // Speed for camera to auto-align behind the boat
    public float autoRealignDelay = 2f;   // Delay (in seconds) before realignment starts

    private float currentYaw = 0f;
    private Vector3 velocity = Vector3.zero;
    private float currentDistance;
    private float lastInputTime = 0f;

    void Start()
    {
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }

        currentDistance = distance;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (!target) return;

        HandleInput();
        UpdateCamera();
    }

    void HandleInput()
    {
        float mouseX = Input.GetAxis("Mouse X");

        // Detect manual input
        if (Mathf.Abs(mouseX) > 0.01f)
        {
            currentYaw += mouseX * sensitivity * orbitSpeedMultiplier;
            lastInputTime = Time.time;
        }

        // Zoom control
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            currentDistance -= scroll * 3f;
            currentDistance = Mathf.Clamp(currentDistance, 5f, 20f);
        }

        // Reset manually
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetCamera();
        }
    }

    void UpdateCamera()
    {
        if (!target) return;

        // Auto realign camera if no input after delay
        if (Time.time - lastInputTime > autoRealignDelay)
        {
            float targetYaw = target.eulerAngles.y;
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * autoRealignSpeed);
        }

        // Calculate rotation (fixed downward pitch)
        Quaternion rotation = Quaternion.Euler(pitchAngle, currentYaw, 0f);

        // Desired camera position behind boat
        Vector3 offset = rotation * new Vector3(0f, height, -currentDistance);
        Vector3 desiredPosition = target.position + offset;

        // Prevent camera from dipping below water
        float minY = waterLevel + minHeightAboveWater;
        if (desiredPosition.y < minY)
        {
            desiredPosition.y = minY;
        }

        // Smooth movement
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothness);

        // Look toward the front of the boat
        Vector3 lookTarget = target.position + target.forward * 4f + Vector3.up * 1.5f;
        transform.LookAt(lookTarget);
    }

    void ResetCamera()
    {
        if (target != null)
        {
            currentYaw = target.eulerAngles.y;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.blue;
            Vector3 waterStart = new Vector3(target.position.x - 10f, waterLevel, target.position.z - 10f);
            Vector3 waterEnd = new Vector3(target.position.x + 10f, waterLevel, target.position.z + 10f);
            Gizmos.DrawLine(waterStart, waterEnd);

            Vector3 lookTarget = target.position + target.forward * 4f + Vector3.up * 1.5f;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lookTarget, 0.4f);
            Gizmos.DrawLine(transform.position, lookTarget);
        }
    }
}
