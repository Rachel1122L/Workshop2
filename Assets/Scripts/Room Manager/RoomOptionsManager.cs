using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class RoomOptionsManager : MonoBehaviourPunCallbacks
{
    [Header("UI Elements")]
    public Dropdown difficultyDropdown;
    public Dropdown terrainDropdown;
    public Button createRoomButton;

    [Header("Room Settings")]
    public string roomId;
    private RoomOptions roomOptions;

    void Start()
    {
        // Generate random room ID
        roomId = GenerateRoomID();
        Debug.Log("Generated Room ID: " + roomId);

        createRoomButton.onClick.AddListener(CreateRoomWithOptions);
    }

    private string GenerateRoomID()
    {
        return System.Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
    }

    public void CreateRoomWithOptions()
    {
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        // Get selected options
        string difficulty = difficultyDropdown.options[difficultyDropdown.value].text;
        string terrain = terrainDropdown.options[terrainDropdown.value].text;

        // Store room options in custom properties
        roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 4;
        roomOptions.IsVisible = false; // Make room private (join by ID only)

        // Use RoomID as the key (not roomId)
        roomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
    {
        { "Difficulty", difficulty },
        { "Terrain", terrain },
        { "RoomID", roomId } // Use "RoomID" as key
    };
        roomOptions.CustomRoomPropertiesForLobby = new string[] { "Difficulty", "Terrain", "RoomID" };

        Debug.Log($"Creating room with - Difficulty: {difficulty}, Terrain: {terrain}, RoomID: {roomId}");

        // Use the generated roomId as the room name
        PhotonNetwork.CreateRoom(roomId, roomOptions);
    }

    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully!");
        // Set the host as the master client who can start the game
        PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        // Go to waiting room
        SceneManager.LoadScene("WaitingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError("Room creation failed: " + message);
        // Regenerate room ID and try again
        roomId = GenerateRoomID();
        Debug.Log("New Room ID: " + roomId);
    }
}