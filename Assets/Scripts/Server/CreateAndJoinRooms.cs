using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.SceneManagement;

public class CreateAndJoinRooms : MonoBehaviourPunCallbacks
{
    public InputField createInput;
    public InputField joinInput;

    void Start()
    {
        // Set content type programmatically
        if (createInput != null)
        {
            createInput.contentType = InputField.ContentType.Alphanumeric;
            createInput.onValueChanged.AddListener(ValidateRoomName);
        }

        if (joinInput != null)
        {
            joinInput.contentType = InputField.ContentType.Alphanumeric;
            joinInput.onValueChanged.AddListener(ValidateRoomName);
        }
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(createInput.text.Trim()))
        {
            PhotonNetwork.CreateRoom(createInput.text);
        }
    }

    public void JoinRoom()
    {
        if (!string.IsNullOrEmpty(joinInput.text.Trim()))
        {
            PhotonNetwork.JoinRoom(joinInput.text);
        }
    }

    public void ValidateRoomName(string input)
    {
        // Remove any spaces and special characters
        string cleaned = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9]", "");

        // Update the appropriate input field
        if (createInput != null && input == createInput.text)
        {
            createInput.text = cleaned;
        }
        if (joinInput != null && input == joinInput.text)
        {
            joinInput.text = cleaned;
        }
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("Game Scene1");
    }
}