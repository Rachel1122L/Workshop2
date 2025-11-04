using UnityEngine;
using Photon.Pun;
using System.Collections;

public class CollectableItem : MonoBehaviourPunCallbacks
{
    [Header("Item Settings")]
    public string itemName = "Trash Item";
    public int itemValue = 1;

    private bool isCollected = false;
    public bool IsCollected => isCollected;

    private Collider itemCollider;
    private Renderer itemRenderer;

    void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemRenderer = GetComponent<Renderer>();

        if (itemCollider != null && !itemCollider.isTrigger)
        {
            itemCollider.isTrigger = true;
        }
    }

    void Start()
    {
        UpdateVisualState();
    }

    public void Collect()
    {
        if (IsCollected) return;

        Debug.Log($"Collect called for {itemName} by player {PhotonNetwork.LocalPlayer.ActorNumber}");

        if (PhotonNetwork.IsConnected)
        {
            // Simple RPC call - no ownership transfer needed
            photonView.RPC("RPC_CollectItem", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber);
        }
        else
        {
            LocalCollect();
        }
    }

    [PunRPC]
    void RPC_CollectItem(int collectingPlayerId)
    {
        if (IsCollected) return;

        Debug.Log($"RPC_CollectItem: {itemName} collected by player {collectingPlayerId}");

        isCollected = true;
        UpdateVisualState();

        // Only the player who collected it adds to their score
        if (PhotonNetwork.LocalPlayer.ActorNumber == collectingPlayerId)
        {
            Debug.Log($"Adding score to local player for {itemName}");

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(itemValue, itemName);
            }
        }

        // All clients hide the item
        StartCoroutine(HideAndDestroyItem());
    }

    private IEnumerator HideAndDestroyItem()
    {
        // Hide immediately
        UpdateVisualState();

        yield return new WaitForSeconds(0.1f);

        // Only master client destroys the network object
        if (PhotonNetwork.IsMasterClient && photonView != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Non-master clients just disable it
            gameObject.SetActive(false);
        }
    }

    private void LocalCollect()
    {
        isCollected = true;
        UpdateVisualState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(itemValue, itemName);
        }

        Destroy(gameObject, 0.1f);
    }

    private void UpdateVisualState()
    {
        if (itemCollider != null)
            itemCollider.enabled = !IsCollected;
        if (itemRenderer != null)
            itemRenderer.enabled = !IsCollected;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsCollected) return;

        BoatCollector boatCollector = other.GetComponent<BoatCollector>();
        if (boatCollector != null && boatCollector.photonView.IsMine)
        {
            boatCollector.SetNearbyCollectable(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsCollected) return;

        BoatCollector boatCollector = other.GetComponent<BoatCollector>();
        if (boatCollector != null && boatCollector.photonView.IsMine)
        {
            boatCollector.ClearNearbyCollectable(this);
        }
    }
}