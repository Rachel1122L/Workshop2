using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeLookCamera : MonoBehaviour
{
    [Header("Camera References")]
    public Transform cameraTransform;
    public Transform playerTransform; // The boat transform

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;
    public float controllerSensitivity = 1.5f;
    public bool invertY = false;

    [Header("Rotation Limits")]
    public float minVerticalAngle = -80f;
    public float maxVerticalAngle = 80f;

    [Header("Smoothing Settings")]
    public float rotationSmoothTime = 0.1f;
    public bool smoothCamera = true;

    [Header("Return to Forward Settings")]
    public bool autoReturnToForward = true;
    public float returnToForwardDelay = 3f;
    public float returnSpeed = 2f;

    [Header("Input Settings")]
    public string horizontalInput = "Mouse X";
    public string verticalInput = "Mouse Y";
    public KeyCode freeLookKey = KeyCode.Mouse1; // Right mouse button
    public bool useController = false;

    // Private variables
    private float cameraYaw = 0f;
    private float cameraPitch = 0f;
    private Vector3 currentRotation;
    private Vector3 rotationVelocity;

    private Quaternion originalCameraRotation;
    private Quaternion targetRotation;
    private float timeSinceLastInput = 0f;
    private bool isFreeLooking = false;

    void Start()
    {
        // If no camera transform is assigned, use this transform
        if (cameraTransform == null)
            cameraTransform = transform;

        // If no player transform is assigned, try to find the boat in parent
        if (playerTransform == null)
            playerTransform = GetComponentInParent<SimpleBoatController>()?.transform;

        if (playerTransform == null)
        {
            Debug.LogWarning("FreeLookCamera: No player transform assigned. Looking for parent with Rigidbody...");
            playerTransform = GetComponentInParent<Rigidbody>()?.transform;
        }

        // Store original rotation
        originalCameraRotation = cameraTransform.localRotation;

        // Initialize camera angles based on current rotation
        Vector3 currentEuler = cameraTransform.localEulerAngles;
        cameraPitch = currentEuler.x;
        cameraYaw = currentEuler.y;

        currentRotation = new Vector3(cameraPitch, cameraYaw);
        targetRotation = cameraTransform.localRotation;
    }

    void Update()
    {
        if (cameraTransform == null || playerTransform == null)
            return;

        HandleInput();
        HandleFreeLook();
        HandleReturnToForward();
    }

    void HandleInput()
    {
        // Check if free look key is pressed (or always active if no key is set)
        bool wasFreeLooking = isFreeLooking;

        if (freeLookKey != KeyCode.None)
        {
            isFreeLooking = Input.GetKey(freeLookKey);
        }
        else
        {
            // If no key is set, free look is always active when there's input
            float lookInput = useController ?
                Mathf.Abs(Input.GetAxis(horizontalInput)) + Mathf.Abs(Input.GetAxis(verticalInput)) :
                Mathf.Abs(Input.GetAxis(horizontalInput)) + Mathf.Abs(Input.GetAxis(verticalInput));

            isFreeLooking = lookInput > 0.1f;
        }

        // Reset timer if input state changed
        if (wasFreeLooking != isFreeLooking)
        {
            timeSinceLastInput = 0f;
        }
    }

    void HandleFreeLook()
    {
        if (isFreeLooking)
        {
            // Get input
            float mouseX = Input.GetAxis(horizontalInput);
            float mouseY = Input.GetAxis(verticalInput);

            // Apply sensitivity
            float sensitivity = useController ? controllerSensitivity : mouseSensitivity;
            mouseX *= sensitivity;
            mouseY *= sensitivity;

            // Invert Y if needed
            if (invertY)
                mouseY = -mouseY;

            // Update camera angles
            cameraYaw += mouseX;
            cameraPitch -= mouseY;

            // Clamp vertical angle
            cameraPitch = Mathf.Clamp(cameraPitch, minVerticalAngle, maxVerticalAngle);

            // Update target rotation
            targetRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);

            // Reset timer since we have input
            timeSinceLastInput = 0f;
        }

        // Apply rotation with smoothing
        if (smoothCamera)
        {
            currentRotation = Vector3.SmoothDamp(currentRotation,
                new Vector3(targetRotation.eulerAngles.x, targetRotation.eulerAngles.y, 0f),
                ref rotationVelocity, rotationSmoothTime);

            cameraTransform.localRotation = Quaternion.Euler(currentRotation.x, currentRotation.y, 0f);
        }
        else
        {
            cameraTransform.localRotation = targetRotation;
        }
    }

    void HandleReturnToForward()
    {
        if (!autoReturnToForward || isFreeLooking)
        {
            timeSinceLastInput = 0f;
            return;
        }

        timeSinceLastInput += Time.deltaTime;

        // Start returning to forward after delay
        if (timeSinceLastInput >= returnToForwardDelay)
        {
            // Smoothly return camera to forward position
            float returnProgress = (timeSinceLastInput - returnToForwardDelay) * returnSpeed;
            returnProgress = Mathf.Clamp01(returnProgress);

            // Smoothly interpolate back to original rotation
            targetRotation = Quaternion.Slerp(targetRotation, originalCameraRotation, returnProgress);

            // Update camera angles to match the interpolated rotation
            Vector3 targetEuler = targetRotation.eulerAngles;
            cameraPitch = targetEuler.x;
            cameraYaw = targetEuler.y;

            // Ensure angles are in proper range
            if (cameraPitch > 180f) cameraPitch -= 360f;
            if (cameraYaw > 180f) cameraYaw -= 360f;
        }
    }

    // Public methods to control the camera from other scripts
    public void SetFreeLook(bool enable)
    {
        isFreeLooking = enable;
        timeSinceLastInput = 0f;
    }

    public void ResetCamera()
    {
        cameraPitch = originalCameraRotation.eulerAngles.x;
        cameraYaw = originalCameraRotation.eulerAngles.y;
        targetRotation = originalCameraRotation;
        currentRotation = new Vector3(cameraPitch, cameraYaw);
        timeSinceLastInput = 0f;

        if (!smoothCamera)
        {
            cameraTransform.localRotation = originalCameraRotation;
        }
    }

    public void SetSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }

    // Getters for current state
    public bool IsFreeLooking()
    {
        return isFreeLooking;
    }

    public Vector2 GetLookAngles()
    {
        return new Vector2(cameraPitch, cameraYaw);
    }

    // Optional: Draw debug info in scene view
    void OnDrawGizmosSelected()
    {
        if (cameraTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(cameraTransform.position,
                cameraTransform.position + cameraTransform.forward * 2f);

            Gizmos.color = Color.yellow;
            if (playerTransform != null)
            {
                Gizmos.DrawLine(cameraTransform.position,
                    playerTransform.position + playerTransform.forward * 3f);
            }
        }
    }
}