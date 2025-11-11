using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class LobbyManager : MonoBehaviour
{
    public void OnCreateRoom()
    {
        // Go to RoomOptions scene to set room settings
        SceneManager.LoadScene("RoomOptions");
    }

    public void OnJoinRoom()
    {
        // Go to JoinRoom scene to enter room ID
        SceneManager.LoadScene("JoinRoom");
    }
}