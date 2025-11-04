using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("Game Stats")]
    public int totalScore = 0;
    public List<CollectableItem> collectedItems = new List<CollectableItem>();

    [Header("UI References")]
    public Text scoreText;
    public Text itemsText;

    [Header("Multiplayer Setup")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Collectable Prefab References")]
    public GameObject[] collectablePrefabs;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        Debug.Log("GameManager Started - Scene: " + SceneManager.GetActiveScene().name);

        // Setup UI
        UpdateUI();

        // Handle multiplayer spawning if we're in a room
        if (PhotonNetwork.InRoom)
        {
            SetupMultiplayer();
        }
    }

    private void SetupMultiplayer()
    {
        Debug.Log("Setting up multiplayer...");
        Debug.Log($"In room: {PhotonNetwork.CurrentRoom.Name} with {PhotonNetwork.CurrentRoom.PlayerCount} players");

        // Setup camera
        SetupCamera();

        // Spawn player
        SpawnPlayer();
    }

    private void SetupCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            Debug.Log("Main camera activated: " + mainCamera.name);
        }
        else
        {
            Debug.LogError("Main camera not found in scene!");
        }
    }

    private void SpawnPlayer()
    {
        if (!PhotonNetwork.IsConnected || !PhotonNetwork.InRoom)
        {
            Debug.LogError("Cannot spawn - not connected to Photon or not in room!");
            return;
        }

        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab not assigned in GameManager!");
            return;
        }

        try
        {
            Vector3 spawnPosition = GetSpawnPosition();
            Debug.Log($"Spawning player at position: {spawnPosition}");

            GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, spawnPosition, Quaternion.identity);

            if (player != null)
            {
                Debug.Log($"Player spawned successfully: {player.name}");

                // Setup camera to follow this player if it's ours
                if (player.GetComponent<PhotonView>().IsMine)
                {
                    SetupPlayerCamera(player);
                }
            }
            else
            {
                Debug.LogError("Failed to spawn player prefab!");
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
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            // Add camera follow component if it doesn't exist
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
            }

            cameraFollow.target = player.transform;
            Debug.Log("Camera following player: " + player.name);
        }
    }

    public void AddScore(int points)
    {
        totalScore += points;
        UpdateUI();

        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Score updated: {totalScore}");
        }
    }

    public void AddCollectedItem(CollectableItem item)
    {
        collectedItems.Add(item);
        UpdateUI();

        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"Item collected: {item.itemName}");
        }
    }

    public void RemoveCollectedItem(CollectableItem item)
    {
        collectedItems.Remove(item);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }

        if (itemsText != null)
        {
            itemsText.text = $"Items: {collectedItems.Count}";
        }
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        Debug.Log("GameManager: Joined room successfully");

        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            SetupMultiplayer();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room, returning to lobby...");

        totalScore = 0;
        collectedItems.Clear();
        UpdateUI();

        SceneManager.LoadScene("Lobby");
    }

    // FIXED: Use public access modifiers for overrides
    public override void OnEnable()
    {
        base.OnEnable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        FindUIReferences();
        UpdateUI();

        if (PhotonNetwork.InRoom && scene.name == "GameScene")
        {
            Invoke(nameof(SetupMultiplayer), 0.1f);
        }
    }

    private void FindUIReferences()
    {
        GameObject scoreObj = GameObject.Find("ScoreText");
        GameObject itemsObj = GameObject.Find("ItemsText");

        if (scoreObj != null) scoreText = scoreObj.GetComponent<Text>();
        if (itemsObj != null) itemsText = itemsObj.GetComponent<Text>();

        Debug.Log($"UI References - Score: {scoreText != null}, Items: {itemsText != null}");
    }

    public GameObject GetCollectablePrefab(string prefabName)
    {
        foreach (GameObject prefab in collectablePrefabs)
        {
            if (prefab.name == prefabName)
            {
                return prefab;
            }
        }
        Debug.LogWarning($"Collectable prefab not found: {prefabName}");
        return null;
    }

    public void SpawnCollectable(string prefabName, Vector3 position)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject prefab = GetCollectablePrefab(prefabName);
            if (prefab != null)
            {
                PhotonNetwork.Instantiate(prefab.name, position, Quaternion.identity);
            }
        }
    }

    // Debug method for testing
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && PhotonNetwork.InRoom)
        {
            Debug.Log("Manual respawn triggered");
            SpawnPlayer();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            AddScore(1);
            GameObject testItem = new GameObject("TestItem");
            CollectableItem collectable = testItem.AddComponent<CollectableItem>();
            collectable.itemName = "TestItem";
            AddCollectedItem(collectable);
            Destroy(testItem);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
    }
}