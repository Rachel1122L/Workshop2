using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.SceneManagement;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;

    [Header("UI References")]
    public Text scoreText;
    public Text itemsText;
    public GameObject scoreboardPanel;
    public Text scoreboardText;

    [Header("Multiplayer Setup")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Collectable Prefab References")]
    public GameObject[] collectablePrefabs;

    // Player-specific data - each player manages their own
    private int localPlayerScore = 0;
    private List<string> localCollectedItems = new List<string>();

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

        // Initialize player properties for THIS player only
        InitializePlayerProperties();

        // Setup camera
        SetupCamera();

        // Spawn player
        SpawnPlayer();
    }

    private void InitializePlayerProperties()
    {
        // Set initial score for THIS player only
        Hashtable initialProps = new Hashtable();
        initialProps["Score"] = 0;
        initialProps["ItemsCount"] = 0;
        initialProps["PlayerName"] = PhotonNetwork.LocalPlayer.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

        localPlayerScore = 0;
        localCollectedItems.Clear();

        Debug.Log($"Initialized properties for player: {PhotonNetwork.LocalPlayer.NickName}");
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

                    // Initialize this player's score UI
                    localPlayerScore = 0;
                    localCollectedItems.Clear();
                    UpdateUI();
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

    // Call this method when a player collects something
    public void AddScore(int points, string itemName = "")
    {
        // Only update for the local player
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        localPlayerScore += points;

        if (!string.IsNullOrEmpty(itemName))
        {
            localCollectedItems.Add(itemName);
        }

        // Update custom properties for this player only
        Hashtable props = new Hashtable();
        props["Score"] = localPlayerScore;
        props["ItemsCount"] = localCollectedItems.Count;
        props["PlayerName"] = PhotonNetwork.LocalPlayer.NickName;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        UpdateUI();
        UpdateScoreboard();

        Debug.Log($"{PhotonNetwork.LocalPlayer.NickName} score: {localPlayerScore}, items: {localCollectedItems.Count}");
    }

    private void UpdateUI()
    {
        // Only update UI for local player
        if (!PhotonNetwork.LocalPlayer.IsLocal) return;

        if (scoreText != null)
        {
            scoreText.text = $"Score: {localPlayerScore}";
        }

        if (itemsText != null)
        {
            itemsText.text = $"Items: {localCollectedItems.Count}";
        }
    }

    private void UpdateScoreboard()
    {
        if (scoreboardText != null && PhotonNetwork.InRoom)
        {
            string scoreboard = "SCOREBOARD:\n";

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int playerScore = player.CustomProperties.ContainsKey("Score") ? (int)player.CustomProperties["Score"] : 0;
                int playerItems = player.CustomProperties.ContainsKey("ItemsCount") ? (int)player.CustomProperties["ItemsCount"] : 0;
                string playerName = player.CustomProperties.ContainsKey("PlayerName") ? (string)player.CustomProperties["PlayerName"] : player.NickName;

                if (player.IsLocal)
                {
                    scoreboard += $"<b>{playerName} (You): {playerScore} pts, {playerItems} items</b>\n";
                }
                else
                {
                    scoreboard += $"{playerName}: {playerScore} pts, {playerItems} items\n";
                }
            }

            scoreboardText.text = scoreboard;
        }
    }

    public void ToggleScoreboard()
    {
        if (scoreboardPanel != null)
        {
            scoreboardPanel.SetActive(!scoreboardPanel.activeSelf);
            if (scoreboardPanel.activeSelf)
            {
                UpdateScoreboard();
            }
        }
    }

    // Photon callbacks
    public override void OnJoinedRoom()
    {
        Debug.Log("GameManager: Joined room successfully");

        // Reset local player data when joining a new room
        localPlayerScore = 0;
        localCollectedItems.Clear();

        if (SceneManager.GetActiveScene().name == "GameScene")
        {
            SetupMultiplayer();
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log("Left room, returning to lobby...");

        localPlayerScore = 0;
        localCollectedItems.Clear();
        UpdateUI();

        SceneManager.LoadScene("Lobby");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // When any player updates their score, refresh the scoreboard
        UpdateScoreboard();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player joined: {newPlayer.NickName}");
        UpdateScoreboard();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player left: {otherPlayer.NickName}");
        UpdateScoreboard();
    }

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
        UpdateScoreboard();

        if (PhotonNetwork.InRoom && scene.name == "GameScene")
        {
            // Reset local player data on scene load
            localPlayerScore = 0;
            localCollectedItems.Clear();
            Invoke(nameof(SetupMultiplayer), 0.1f);
        }
    }

    private void FindUIReferences()
    {
        GameObject scoreObj = GameObject.Find("ScoreText");
        GameObject itemsObj = GameObject.Find("ItemsText");
        GameObject scoreboardObj = GameObject.Find("ScoreboardPanel");
        GameObject scoreboardTextObj = GameObject.Find("ScoreboardText");

        if (scoreObj != null) scoreText = scoreObj.GetComponent<Text>();
        if (itemsObj != null) itemsText = itemsObj.GetComponent<Text>();
        if (scoreboardObj != null) scoreboardPanel = scoreboardObj;
        if (scoreboardTextObj != null) scoreboardText = scoreboardTextObj.GetComponent<Text>();

        Debug.Log($"UI References - Score: {scoreText != null}, Items: {itemsText != null}");
    }

    // Debug method for testing
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && PhotonNetwork.InRoom)
        {
            Debug.Log("Manual respawn triggered");
            SpawnPlayer();
        }

        if (Input.GetKeyDown(KeyCode.T) && PhotonNetwork.LocalPlayer.IsLocal)
        {
            AddScore(1, "TestItem");
            Debug.Log($"Local player score: {localPlayerScore}");
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleScoreboard();
        }
    }

    // Public getters for other scripts to access local player data
    public int GetLocalPlayerScore()
    {
        return localPlayerScore;
    }

    public int GetLocalPlayerItemsCount()
    {
        return localCollectedItems.Count;
    }

    public string GetLocalPlayerName()
    {
        return PhotonNetwork.LocalPlayer.NickName;
    }
}