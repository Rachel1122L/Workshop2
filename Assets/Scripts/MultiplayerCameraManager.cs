using UnityEngine;
using Cinemachine;
using Photon.Pun;

public class MultiplayerCameraManager : MonoBehaviourPunCallbacks
{
    public GameObject playerCameraPrefab;
    public Vector3 cameraPositionOffset = new Vector3(0f, 2f, -5f);
    public float fieldOfView = 60f;

    private CinemachineVirtualCamera playerVirtualCamera;
    private GameObject cameraGameObject;

    void Start()
    {
        if (!photonView.IsMine) return;

        CreatePlayerCamera();
    }

    private void CreatePlayerCamera()
    {
        if (playerCameraPrefab == null) return;

        cameraGameObject = Instantiate(playerCameraPrefab, Vector3.zero, Quaternion.identity);
        playerVirtualCamera = cameraGameObject.GetComponentInChildren<CinemachineVirtualCamera>();

        if (playerVirtualCamera != null)
        {
            playerVirtualCamera.Follow = transform;
            playerVirtualCamera.LookAt = transform;
            playerVirtualCamera.m_Lens.FieldOfView = fieldOfView;

            var transposer = playerVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = cameraPositionOffset;
            }
        }

        SetupAudioListener();
    }

    private void SetupAudioListener()
    {
        AudioListener newAudioListener = cameraGameObject.GetComponentInChildren<AudioListener>();
        if (newAudioListener == null) return;

        AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
        foreach (AudioListener listener in allListeners)
        {
            listener.enabled = (listener == newAudioListener);
        }
    }

    void OnDestroy()
    {
        if (cameraGameObject != null && photonView.IsMine)
        {
            Destroy(cameraGameObject);
        }
    }
}