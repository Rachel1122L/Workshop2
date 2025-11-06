using UnityEngine;
using Photon.Pun;

public class SpawnBoats : MonoBehaviourPunCallbacks
{
    [Header("Boat Setup")]
    public GameObject playerPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    private void Start()
    {
        Debug.Log("=== SPAWN BOATS START ===");
        Debug.Log($"Photon Connected: {PhotonNetwork.IsConnected}");
        Debug.Log($"In Room: {PhotonNetwork.InRoom}");
        Debug.Log($"Player Count: {(PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom.PlayerCount : 0)}");

        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("Cannot spawn - not connected or not in room!");
            return;
        }

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        try
        {
            Vector3 spawnPosition = GetSpawnPosition();
            Debug.Log($"Spawning at position: {spawnPosition}");

            // Spawn the player
            GameObject player = PhotonNetwork.Instantiate(
                playerPrefab.name,
                spawnPosition,
                Quaternion.identity
            );

            if (player != null)
            {
                Debug.Log($"Successfully spawned: {player.name}");

                // Make sure the player is active
                player.SetActive(true);

                // If this is the local player, setup camera
                if (player.GetComponent<PhotonView>().IsMine)
                {
                    SetupPlayerCamera(player);
                }
            }
            else
            {
                Debug.LogError("PhotonNetwork.Instantiate returned null!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Spawning failed: {e.Message}");
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned, using default position");
            return Vector3.zero;
        }

        int playerNumber = PhotonNetwork.LocalPlayer.ActorNumber - 1;
        int spawnIndex = playerNumber % spawnPoints.Length;

        Debug.Log($"Player {PhotonNetwork.LocalPlayer.ActorNumber} using spawn point {spawnIndex}");
        return spawnPoints[spawnIndex].position;
    }

    private void SetupPlayerCamera(GameObject player)
    {
        // Find the main camera and make it follow the player
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Add or get camera follow component
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            cameraFollow.target = player.transform;
            Debug.Log("Camera setup complete for local player");
        }
        else
        {
            Debug.LogError("Main camera not found!");
        }
    }
}