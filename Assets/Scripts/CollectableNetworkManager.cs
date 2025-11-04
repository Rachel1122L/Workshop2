using UnityEngine;
using Photon.Pun;

public class CollectableNetworkManager : MonoBehaviourPunCallbacks
{
    public static CollectableNetworkManager Instance;

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

    // Call this method for scene-based collectables
    public void RegisterSceneCollectable(CollectableItem collectable)
    {
        if (PhotonNetwork.IsConnected && collectable.photonView != null && collectable.photonView.ViewID == 0)
        {
            collectable.photonView.ViewID = PhotonNetwork.AllocateViewID(false);
            Debug.Log($"Assigned ViewID {collectable.photonView.ViewID} to {collectable.itemName}");
        }
    }
}