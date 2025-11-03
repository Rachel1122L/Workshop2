using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
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
    private bool photonComponentsInitialized = false;

    void Awake()
    {
        itemCollider = GetComponent<Collider>();
        itemRenderer = GetComponent<Renderer>();

        // Ensure collider is trigger
        if (itemCollider != null && !itemCollider.isTrigger)
        {
            itemCollider.isTrigger = true;
        }
    }

    void Start()
    {
        InitializePhotonComponents();

        if (CollectableNetworkManager.Instance != null)
        {
            CollectableNetworkManager.Instance.RegisterSceneCollectable(this);
        }

        UpdateVisualState();
    }

    private void InitializePhotonComponents()
    {
        if (photonComponentsInitialized) return;

        if (PhotonNetwork.IsConnected)
        {
            PhotonView pv = GetComponent<PhotonView>();
            if (pv == null)
            {
                pv = gameObject.AddComponent<PhotonView>();
                pv.OwnershipTransfer = OwnershipOption.Takeover;
                pv.Synchronization = ViewSynchronization.UnreliableOnChange;

                // Assign a view ID if needed (for instantiated objects)
                if (pv.ViewID == 0)
                {
                    pv.ViewID = PhotonNetwork.AllocateViewID(false);
                }
            }

            PhotonTransformViewClassic ptv = GetComponent<PhotonTransformViewClassic>();
            if (ptv == null)
            {
                ptv = gameObject.AddComponent<PhotonTransformViewClassic>();
            }

            if (pv.ObservedComponents == null || pv.ObservedComponents.Count == 0)
            {
                pv.ObservedComponents = new System.Collections.Generic.List<Component> { ptv };
            }
            else if (!pv.ObservedComponents.Contains(ptv))
            {
                pv.ObservedComponents.Add(ptv);
            }

            Debug.Log($"Photon components initialized for {itemName}. ViewID: {pv.ViewID}");
        }

        photonComponentsInitialized = true;
    }

    public void Collect()
    {
        if (IsCollected)
        {
            Debug.Log($"{itemName} already collected, ignoring collect request");
            return;
        }

        Debug.Log($"Collect called for {itemName}. Network: {PhotonNetwork.IsConnected}");

        if (PhotonNetwork.IsConnected)
        {
            // Double-check photon components
            if (photonView == null || photonView.ViewID == 0)
            {
                Debug.LogError($"PhotonView not properly initialized for {itemName}! Falling back to local collection.");
                LocalCollect();
                return;
            }

            // Request ownership then collect
            photonView.RequestOwnership();
            StartCoroutine(CollectAfterOwnership());
        }
        else
        {
            LocalCollect();
        }
    }

    private IEnumerator CollectAfterOwnership()
    {
        // Wait for ownership transfer
        yield return new WaitForSeconds(0.1f);

        if (photonView != null && photonView.ViewID != 0)
        {
            Debug.Log($"Calling RPC_CollectItem for {itemName}. ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");
            photonView.RPC("RPC_CollectItem", RpcTarget.AllBuffered);
        }
        else
        {
            Debug.LogError($"Cannot call RPC - PhotonView invalid for {itemName}. ViewID: {photonView?.ViewID}");
            // Fallback to local collection
            LocalCollect();
        }
    }

    [PunRPC]
    void RPC_CollectItem()
    {
        if (IsCollected)
        {
            Debug.Log($"{itemName} already collected in RPC");
            return;
        }

        Debug.Log($"RPC_CollectItem executed for {itemName}");

        isCollected = true;
        UpdateVisualState();

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCollectedItem(this);
            GameManager.Instance.AddScore(itemValue);
        }
        else
        {
            Debug.LogWarning("GameManager instance not found!");
        }

        // Only master client destroys the object after a delay
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(DestroyAfterDelay(2f));
        }
        else
        {
            Debug.Log("Not master client, waiting for master to destroy object");
        }
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (photonView != null && PhotonNetwork.IsMasterClient && gameObject != null)
        {
            Debug.Log($"Master client destroying {itemName}");
            PhotonNetwork.Destroy(gameObject);
        }
    }

    private void LocalCollect()
    {
        Debug.Log($"Local collect for {itemName}");
        isCollected = true;
        UpdateVisualState();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddCollectedItem(this);
            GameManager.Instance.AddScore(itemValue);
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
        if (boatCollector != null)
        {
            Debug.Log($"Trigger entered with boat: {other.gameObject.name}. Boat is mine: {boatCollector.photonView.IsMine}");
            boatCollector.SetNearbyCollectable(this);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        BoatCollector boatCollector = other.GetComponent<BoatCollector>();
        if (boatCollector != null)
        {
            Debug.Log($"Trigger exited with boat: {other.gameObject.name}");
            boatCollector.ClearNearbyCollectable(this);
        }
    }

    // Called when the object is instantiated via Photon
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log($"CollectableItem {itemName} instantiated via Photon. ViewID: {photonView.ViewID}");
        InitializePhotonComponents();
    }

#if UNITY_EDITOR
    [ContextMenu("Setup Photon Components")]
    private void SetupPhotonComponents()
    {
        InitializePhotonComponents();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}