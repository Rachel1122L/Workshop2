using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.SceneManagement;

public class JoinRoomManager : MonoBehaviourPunCallbacks
{
    public InputField roomIdInput;
    public Button joinButton;
    public Text statusText;

    void Start()
    {
        joinButton.onClick.AddListener(JoinRoomById);
        roomIdInput.onValueChanged.AddListener(ValidateRoomId);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    private void ValidateRoomId(string roomId)
    {
        joinButton.interactable = !string.IsNullOrEmpty(roomId.Trim());
    }

    public void JoinRoomById()
    {
        string roomId = roomIdInput.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(roomId))
        {
            statusText.text = "Please enter a Room ID";
            return;
        }

        statusText.text = "Joining room...";
        joinButton.interactable = false;

        PhotonNetwork.JoinRoom(roomId);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Successfully joined room: " + PhotonNetwork.CurrentRoom.Name);
        SceneManager.LoadScene("WaitingRoom");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        statusText.text = "Failed to join room: " + message;
        joinButton.interactable = true;
        Debug.LogError("Join room failed: " + message);
    }

    public void BackToLobby()
    {
        SceneManager.LoadScene("Lobby");
    }
}