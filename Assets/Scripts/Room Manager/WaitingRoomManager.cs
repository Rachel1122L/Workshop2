using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class WaitingRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Text roomInfoText;
    public Text playerListText;
    public Button startGameButton;

    [Header("Team Selection UI")]
    public Button teamAButton;
    public Button teamBButton;
    public Image teamAHighlight;
    public Image teamBHighlight;
    public Text teamAText;
    public Text teamBText;

    [Header("Team Settings")]
    public List<Player> teamA = new List<Player>();
    public List<Player> teamB = new List<Player>();

    private Dictionary<Player, string> playerTeams = new Dictionary<Player, string>();
    private string localPlayerTeam = "";
    private PhotonView photonViewComponent;

    void Start()
    {
        // Get the PhotonView component
        photonViewComponent = GetComponent<PhotonView>();
        if (photonViewComponent == null)
        {
            Debug.LogError("PhotonView component not found on this GameObject!");
            return;
        }

        InitializeUI();
        UpdateRoomInfo();
        UpdatePlayerList();

        // If we're joining an existing room, request current team data
        if (!PhotonNetwork.IsMasterClient)
        {
            photonViewComponent.RPC("RequestTeamData", RpcTarget.MasterClient);
        }
    }

    private void InitializeUI()
    {
        // Only host can start the game
        if (startGameButton != null)
        {
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
            startGameButton.onClick.AddListener(StartGame);
        }

        // Team selection buttons
        if (teamAButton != null)
            teamAButton.onClick.AddListener(() => SelectTeam("TeamA"));

        if (teamBButton != null)
            teamBButton.onClick.AddListener(() => SelectTeam("TeamB"));

        // Initialize highlights
        if (teamAHighlight != null)
            teamAHighlight.gameObject.SetActive(false);

        if (teamBHighlight != null)
            teamBHighlight.gameObject.SetActive(false);

        UpdateTeamDisplay();
    }

    private void SelectTeam(string team)
    {
        if (photonViewComponent == null)
        {
            Debug.LogError("PhotonView is null! Cannot select team.");
            return;
        }

        if (localPlayerTeam == team) return; // Already in this team

        localPlayerTeam = team;
        UpdateLocalTeamHighlight();

        // Update team selection via RPC to ALL players (including host)
        photonViewComponent.RPC("RPC_SelectTeam", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer, team);
    }

    [PunRPC]
    private void RPC_SelectTeam(Player player, string team)
    {
        Debug.Log($"RPC_SelectTeam called: Player {player.ActorNumber} selected {team}");

        if (!playerTeams.ContainsKey(player))
        {
            playerTeams.Add(player, team);
        }
        else
        {
            playerTeams[player] = team;
        }

        UpdateTeamLists();
        UpdatePlayerList();
        CheckGameStartConditions();
    }

    // New RPC method to request team data from master client
    [PunRPC]
    private void RequestTeamData()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Send current team data to the requesting player
        foreach (var kvp in playerTeams)
        {
            photonViewComponent.RPC("RPC_SelectTeam", RpcTarget.Others, kvp.Key, kvp.Value);
        }
    }

    private void UpdateLocalTeamHighlight()
    {
        if (teamAHighlight != null)
            teamAHighlight.gameObject.SetActive(localPlayerTeam == "TeamA");

        if (teamBHighlight != null)
            teamBHighlight.gameObject.SetActive(localPlayerTeam == "TeamB");
    }

    private void UpdateTeamLists()
    {
        teamA.Clear();
        teamB.Clear();

        foreach (var kvp in playerTeams)
        {
            if (kvp.Value == "TeamA")
            {
                teamA.Add(kvp.Key);
            }
            else if (kvp.Value == "TeamB")
            {
                teamB.Add(kvp.Key);
            }
        }

        UpdateTeamDisplay();
    }

    private void UpdateTeamDisplay()
    {
        if (teamAText != null)
            teamAText.text = $"Team A\n({teamA.Count} players)";

        if (teamBText != null)
            teamBText.text = $"Team B\n({teamB.Count} players)";
    }

    private void UpdateRoomInfo()
    {
        if (roomInfoText == null) return;

        if (PhotonNetwork.CurrentRoom != null)
        {
            string difficulty = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Difficulty") ?
                PhotonNetwork.CurrentRoom.CustomProperties["Difficulty"] as string : "Not Set";
            string terrain = PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("Terrain") ?
                PhotonNetwork.CurrentRoom.CustomProperties["Terrain"] as string : "Not Set";

            // Get Room ID directly from room name (this is what we used when creating the room)
            string roomId = PhotonNetwork.CurrentRoom.Name;

            roomInfoText.text = $"Room ID: {roomId}\n" +
                              $"Difficulty: {difficulty}\n" +
                              $"Terrain: {terrain}\n" +
                              $"Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}";
        }
        else
        {
            roomInfoText.text = "Room information not available";
        }
    }

    private void UpdatePlayerList()
    {
        if (playerListText == null) return;

        string playerList = "Players:\n";

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            string team = "No Team";
            if (playerTeams.ContainsKey(player))
            {
                team = playerTeams[player];
            }

            string playerName = string.IsNullOrEmpty(player.NickName) ? $"Player {player.ActorNumber}" : player.NickName;
            string masterIndicator = player.IsMasterClient ? " (Host)" : "";

            playerList += $"{playerName} - {team}{masterIndicator}\n";
        }

        playerListText.text = playerList;

        // Debug log to see what's happening
        Debug.Log($"Updated player list. Total players: {PhotonNetwork.PlayerList.Length}, Teams recorded: {playerTeams.Count}");
    }

    private void CheckGameStartConditions()
    {
        if (!PhotonNetwork.IsMasterClient || startGameButton == null) return;

        // Check if all players have selected a team
        bool allPlayersHaveTeams = playerTeams.Count >= PhotonNetwork.CurrentRoom.PlayerCount;

        // Optional: Check if teams are balanced (you can remove this if not needed)
        bool teamsAreBalanced = Mathf.Abs(teamA.Count - teamB.Count) <= 2; // Allow 2 player difference

        startGameButton.interactable = allPlayersHaveTeams && teamsAreBalanced;

        Debug.Log($"Game start conditions - All have teams: {allPlayersHaveTeams}, Balanced: {teamsAreBalanced}, Can start: {startGameButton.interactable}");
    }

    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Store team information in room properties
        ExitGames.Client.Photon.Hashtable teamProperties = new ExitGames.Client.Photon.Hashtable();

        foreach (var kvp in playerTeams)
        {
            teamProperties[kvp.Key.ActorNumber.ToString()] = kvp.Value;
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(teamProperties);

        // Load game scene for all players
        PhotonNetwork.LoadLevel("GameScene1");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Player {newPlayer.ActorNumber} entered the room");

        UpdateRoomInfo();
        UpdatePlayerList();
        CheckGameStartConditions();

        // If master client, send current team data to the new player
        if (PhotonNetwork.IsMasterClient)
        {
            foreach (var kvp in playerTeams)
            {
                photonViewComponent.RPC("RPC_SelectTeam", newPlayer, kvp.Key, kvp.Value);
            }
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"Player {otherPlayer.ActorNumber} left the room");

        if (playerTeams.ContainsKey(otherPlayer))
        {
            playerTeams.Remove(otherPlayer);
        }

        // If local player was the one who left, clear their team
        if (otherPlayer == PhotonNetwork.LocalPlayer)
        {
            localPlayerTeam = "";
            UpdateLocalTeamHighlight();
        }

        UpdateTeamLists();
        UpdateRoomInfo();
        UpdatePlayerList();
        CheckGameStartConditions();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"Master client switched to {newMasterClient.ActorNumber}");

        if (startGameButton != null)
            startGameButton.interactable = PhotonNetwork.IsMasterClient;
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("Lobby");
    }

    // Debug method to force team sync (can be called from a button if needed)
    public void DebugForceTeamSync()
    {
        if (photonViewComponent != null)
        {
            photonViewComponent.RPC("RequestTeamData", RpcTarget.MasterClient);
        }
    }
}