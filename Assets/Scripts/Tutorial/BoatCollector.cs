using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class BoatCollector : MonoBehaviourPunCallbacks
{
    [Header("Collection Settings")]
    public float collectionRange = 3f;
    public KeyCode collectKey = KeyCode.E;
    public KeyCode dropKey = KeyCode.Q;

    [Header("UI References")]
    public GameObject collectPromptUI;
    public Text collectPromptText;
    public Text inventoryText;
    public AudioClip collectSound;
    public AudioClip dropSound;

    [Header("Inventory")]
    public List<CollectableItem> trashInventory = new List<CollectableItem>();
    public int maxInventorySize = 5;

    public CollectableItem nearbyCollectable;
    public bool nearRecycleBin = false;
    private GameObject currentRecycleBin;
    private AudioSource audioSource;
    private bool isInitialized = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }

        UpdateInventoryUI();
        isInitialized = true;
    }

    void Update()
    {
        if (!photonView.IsMine) return;

        // Collect trash
        if (Input.GetKeyDown(collectKey) && nearbyCollectable != null)
        {
            CollectItem();
        }

        // Drop trash at recycle bin
        if (Input.GetKeyDown(dropKey) && nearRecycleBin && trashInventory.Count > 0)
        {
            DropTrashAtRecycleBin();
        }

        UpdateCollectPrompt();
        UpdateInventoryUI();
    }

    public void SetNearbyCollectable(CollectableItem collectable)
    {
        if (collectable != null && !collectable.IsCollected && nearbyCollectable != collectable)
        {
            nearbyCollectable = collectable;
        }
    }

    public void ClearNearbyCollectable(CollectableItem collectable)
    {
        if (nearbyCollectable == collectable)
        {
            nearbyCollectable = null;
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
        }
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected && trashInventory.Count < maxInventorySize)
        {
            // Play sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // Add to inventory instead of destroying
            CollectableItem collectedItem = nearbyCollectable;
            trashInventory.Add(collectedItem);

            // Hide the collected item but don't destroy it
            collectedItem.gameObject.SetActive(false);
            collectedItem.IsCollected = true;

            // Clear reference and hide prompt
            nearbyCollectable = null;

            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }

            Debug.Log($"Collected: {collectedItem.itemName} - Inventory: {trashInventory.Count}/{maxInventorySize}");
        }
    }

    private void DropTrashAtRecycleBin()
    {
        if (trashInventory.Count > 0 && currentRecycleBin != null)
        {
            // Play drop sound
            if (dropSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(dropSound);
            }

            // Get the last collected trash
            CollectableItem trashToDrop = trashInventory[trashInventory.Count - 1];
            trashInventory.RemoveAt(trashInventory.Count - 1);

            // Show the trash at recycle bin position
            trashToDrop.gameObject.SetActive(true);
            Vector3 dropPosition = currentRecycleBin.transform.position +
                                 Vector3.up * 1.5f +
                                 Random.insideUnitSphere * 0.5f;
            trashToDrop.transform.position = dropPosition;

            // Reset collected state
            trashToDrop.IsCollected = false;

            Debug.Log($"Dropped {trashToDrop.itemName} at recycle bin - Inventory: {trashInventory.Count}/{maxInventorySize}");
        }
    }

    private void UpdateCollectPrompt()
    {
        if (collectPromptUI != null && isInitialized)
        {
            bool showPrompt = nearbyCollectable != null &&
                            !nearbyCollectable.IsCollected &&
                            IsCollectableInRange(nearbyCollectable) &&
                            trashInventory.Count < maxInventorySize;

            collectPromptUI.SetActive(showPrompt);

            if (showPrompt && collectPromptText != null)
            {
                collectPromptText.text = $"Press {collectKey} to collect {nearbyCollectable.itemName}";
            }
        }
    }

    private void UpdateInventoryUI()
    {
        if (inventoryText != null)
        {
            inventoryText.text = $"Trash: {trashInventory.Count}/{maxInventorySize}";

            // Show drop hint when near recycle bin
            if (nearRecycleBin && trashInventory.Count > 0)
            {
                inventoryText.text += $"\nPress {dropKey} to drop at recycle bin";
            }
        }
    }

    private bool IsCollectableInRange(CollectableItem collectable)
    {
        if (collectable == null) return false;
        float distance = Vector3.Distance(transform.position, collectable.transform.position);
        return distance <= collectionRange;
    }

    // Recycle Bin Detection
    void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("RecycleBin"))
        {
            nearRecycleBin = true;
            currentRecycleBin = other.gameObject;
            Debug.Log("Near recycle bin - ready to drop trash");
        }

        if (other.CompareTag("Collectable"))
        {
            CollectableItem collectable = other.GetComponent<CollectableItem>();
            if (collectable != null && !collectable.IsCollected)
            {
                SetNearbyCollectable(collectable);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!photonView.IsMine) return;

        if (other.CompareTag("RecycleBin"))
        {
            nearRecycleBin = false;
            currentRecycleBin = null;
            Debug.Log("Left recycle bin area");
        }

        if (other.CompareTag("Collectable"))
        {
            CollectableItem collectable = other.GetComponent<CollectableItem>();
            if (collectable != null)
            {
                ClearNearbyCollectable(collectable);
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;
        CheckForCollectablesWithOverlap();
    }

    private void CheckForCollectablesWithOverlap()
    {
        if (nearbyCollectable != null)
        {
            if (nearbyCollectable == null ||
                nearbyCollectable.IsCollected ||
                !IsCollectableInRange(nearbyCollectable))
            {
                ClearNearbyCollectable(nearbyCollectable);
                return;
            }
        }

        if (nearbyCollectable == null)
        {
            FindNewCollectables();
        }
    }

    private void FindNewCollectables()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectionRange);
        CollectableItem closestCollectable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.CompareTag("Collectable"))
            {
                CollectableItem collectable = hitCollider.GetComponent<CollectableItem>();
                if (collectable != null && !collectable.IsCollected)
                {
                    float distance = Vector3.Distance(transform.position, collectable.transform.position);
                    if (distance < closestDistance && distance <= collectionRange)
                    {
                        closestCollectable = collectable;
                        closestDistance = distance;
                    }
                }
            }
        }

        if (closestCollectable != null)
        {
            SetNearbyCollectable(closestCollectable);
        }
    }

    void OnDestroy()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}

// Original Code//
/*using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BoatCollector : MonoBehaviour
{
    [Header("Collection Settings")]
    public float collectionRange = 3f;
    public KeyCode collectKey = KeyCode.E;

    [Header("UI References")]
    public GameObject collectPromptUI;
    public Text collectPromptText;
    public AudioClip collectSound;

    public CollectableItem nearbyCollectable;
    private AudioSource audioSource;
    private bool isInitialized = false;

    void Start()
    {
        // Get or add AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Hide prompt initially
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }

        isInitialized = true;
    }

    void Update()
    {
        // Check for collection input
        if (Input.GetKeyDown(collectKey) && nearbyCollectable != null)
        {
            CollectItem();
        }

        // Update UI prompt
        UpdateCollectPrompt();
    }

    public void SetNearbyCollectable(CollectableItem collectable)
    {
        // Only set if not already collected and not already the current collectable
        if (collectable != null && !collectable.IsCollected && nearbyCollectable != collectable)
        {
            nearbyCollectable = collectable;
            Debug.Log($"Nearby collectable set: {collectable.itemName}");
        }
    }

    public void ClearNearbyCollectable(CollectableItem collectable)
    {
        if (nearbyCollectable == collectable)
        {
            nearbyCollectable = null;
            Debug.Log($"Nearby collectable cleared: {collectable.itemName}");

            // Immediately hide prompt when clearing
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }
        }
    }

    private void CollectItem()
    {
        if (nearbyCollectable != null && !nearbyCollectable.IsCollected)
        {
            // Play sound
            if (collectSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(collectSound);
            }

            // Collect the item
            nearbyCollectable.Collect();

            // Clear reference and hide prompt
            CollectableItem collectedItem = nearbyCollectable;
            nearbyCollectable = null;

            // Hide prompt immediately after collection
            if (collectPromptUI != null)
            {
                collectPromptUI.SetActive(false);
            }

            Debug.Log($"Collected: {collectedItem.itemName}");
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
                collectPromptText.text = $"Press {collectKey} to collect {nearbyCollectable.itemName}";
            }
        }
    }

    private bool IsCollectableInRange(CollectableItem collectable)
    {
        if (collectable == null) return false;

        float distance = Vector3.Distance(transform.position, collectable.transform.position);
        return distance <= collectionRange;
    }

    void FixedUpdate()
    {
        // This backup system should check if we lost our current collectable
        CheckForCollectablesWithOverlap();
    }

    private void CheckForCollectablesWithOverlap()
    {
        // If we have a current collectable, verify it's still in range and valid
        if (nearbyCollectable != null)
        {
            // Check if collectable was destroyed, collected, or out of range
            if (nearbyCollectable == null ||
                nearbyCollectable.IsCollected ||
                !IsCollectableInRange(nearbyCollectable))
            {
                ClearNearbyCollectable(nearbyCollectable);
                return; // Don't look for new ones immediately
            }
        }

        // Only look for new collectables if we don't have a current valid one
        if (nearbyCollectable == null)
        {
            FindNewCollectables();
        }
    }

    private void FindNewCollectables()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, collectionRange);
        CollectableItem closestCollectable = null;
        float closestDistance = Mathf.Infinity;

        foreach (var hitCollider in hitColliders)
        {
            CollectableItem collectable = hitCollider.GetComponent<CollectableItem>();
            if (collectable != null && !collectable.IsCollected)
            {
                float distance = Vector3.Distance(transform.position, collectable.transform.position);
                if (distance < closestDistance && distance <= collectionRange)
                {
                    closestCollectable = collectable;
                    closestDistance = distance;
                }
            }
        }

        if (closestCollectable != null)
        {
            SetNearbyCollectable(closestCollectable);
        }
    }

    // Handle cases where collectable is destroyed without triggering collision
    void OnDestroy()
    {
        if (collectPromptUI != null)
        {
            collectPromptUI.SetActive(false);
        }
    }

    // Visualize collection range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, collectionRange);
    }
}
*/