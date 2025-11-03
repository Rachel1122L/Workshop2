using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;

public class BoatCollector : MonoBehaviourPunCallbacks
{
    [Header("Collection Settings")]
    public float collectionRange = 3f;
    public KeyCode collectKey = KeyCode.E;

    [Header("UI References")]
    public GameObject collectPromptUI;
    public Text collectPromptText;
    public AudioClip collectSound;

    [Header("Manual Detection")]
    public LayerMask collectableLayerMask = 1 << 0; // Default layer

    private CollectableItem nearbyCollectable;
    private AudioSource audioSource;
    private bool isInitialized = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }

        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }

        isInitialized = true;
        Debug.Log($"BoatCollector initialized. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
    }

    void Update()
    {
        // Only local player can collect items
        if (!photonView.IsMine)
        {
            return;
        }

        // Manual detection every frame
        FindNearbyCollectables();

        if (Input.GetKeyDown(collectKey) && nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            Debug.Log($"Collect key pressed. Nearby collectable: {nearbyCollectable.itemName}");
            CollectItem();
        }

        UpdateCollectPrompt();
    }

    private void FindNearbyCollectables()
    {
        CollectableItem closestCollectable = null;
        float closestDistance = Mathf.Infinity;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectionRange, collectableLayerMask);

        foreach (var hitCollider in hitColliders)
        {
            CollectableItem collectable = hitCollider.GetComponent<CollectableItem>();
            if (collectable != null && !collectable.IsCollected)
            {
                float distance = Vector3.Distance(transform.position, collectable.transform.position);
                if (distance <= collectionRange && distance < closestDistance)
                {
                    closestCollectable = collectable;
                    closestDistance = distance;
                }
            }
        }

        // Only update if we found a new closest collectable
        if (closestCollectable != null && closestCollectable != nearbyCollectable)
        {
            Debug.Log($"Found collectable: {closestCollectable.itemName} at distance: {closestDistance:F2}");
            SetNearbyCollectable(closestCollectable);
        }
        else if (closestCollectable == null && nearbyCollectable != null)
        {
            // Clear if no collectables in range
            ClearNearbyCollectable(nearbyCollectable);
        }
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            Debug.Log($"Attempting to collect: {nearbyCollectable.itemName}");

            // Play sound locally
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // This will handle network collection
            nearbyCollectable.Collect();

            // Don't clear immediately - let the collection process handle it
            // The collection coroutine will handle clearing after successful collection

            // Hide prompt immediately
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
        }
        else
        {
            Debug.Log($"Cannot collect - nearbyCollectable is null or already collected");
        }
    }

    public void SetNearbyCollectable(CollectableItem collectable)
    {
        if (collectable != null && !collectable.IsCollected && nearbyCollectable != collectable)
        {
            nearbyCollectable = collectable;
            Debug.Log($"Nearby collectable set: {collectable.itemName}. Distance: {Vector3.Distance(transform.position, collectable.transform.position):F2}");
        }
    }

    public void ClearNearbyCollectable(CollectableItem collectable)
    {
        if (nearbyCollectable == collectable)
        {
            Debug.Log($"Clearing nearby collectable: {collectable?.itemName}");
            nearbyCollectable = null;
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
        }
    }

    private void UpdateCollectPrompt()
    {
        if (collectPromptUI != null && isInitialized)
        {
            bool showPrompt = nearbyCollectable != null &&
                            !nearbyCollectable.IsCollected &&
                            IsCollectableInRange(nearbyCollectable);

            collectPromptUI.SetActive(showPrompt);

            if (showPrompt && collectPromptText != null)
            {
                float distance = Vector3.Distance(transform.position, nearbyCollectable.transform.position);
                collectPromptText.text = $"Press {collectKey} to collect {nearbyCollectable.itemName} ({distance:F1}m)";
            }
        }
    }

    private bool IsCollectableInRange(CollectableItem collectable)
    {
        if (collectable == null) return false;
        float distance = Vector3.Distance(transform.position, collectable.transform.position);
        bool inRange = distance <= collectionRange;

        if (!inRange && nearbyCollectable == collectable)
        {
            Debug.Log($"Collectable {collectable.itemName} out of range: {distance:F2} > {collectionRange}");
            ClearNearbyCollectable(collectable);
        }

        return inRange;
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        // Additional validation for current nearby collectable
        if (nearbyCollectable != null && (!IsCollectableInRange(nearbyCollectable) || nearbyCollectable.IsCollected))
        {
            ClearNearbyCollectable(nearbyCollectable);
        }
    }

    void OnDestroy()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    void OnGUI()
    {
        if (photonView.IsMine)
        {
            GUI.Label(new Rect(10, 10, 400, 20), $"Nearby Collectable: {(nearbyCollectable != null ? nearbyCollectable.itemName : "None")}");
            GUI.Label(new Rect(10, 30, 400, 20), $"Distance: {(nearbyCollectable != null ? Vector3.Distance(transform.position, nearbyCollectable.transform.position).ToString("F2") : "N/A")}");
            GUI.Label(new Rect(10, 50, 400, 20), $"Is Collected: {(nearbyCollectable != null ? nearbyCollectable.IsCollected.ToString() : "N/A")}");
            GUI.Label(new Rect(10, 70, 400, 20), $"Range: {collectionRange}");
            GUI.Label(new Rect(10, 90, 400, 20), $"Boat ViewID: {photonView.ViewID}");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Collection range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);

        // Line to nearby collectable
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            float distance = Vector3.Distance(transform.position, nearbyCollectable.transform.position);
            Gizmos.color = distance <= collectionRange ? Color.green : Color.yellow;
            Gizmos.DrawLine(transform.position, nearbyCollectable.transform.position);

            // Draw sphere at collectable position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(nearbyCollectable.transform.position, 0.3f);
        }
    }
}