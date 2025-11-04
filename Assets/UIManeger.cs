using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI References - TextMeshPro")]
    public TextMeshProUGUI inventoryText;
    public TextMeshProUGUI collectPromptText;
    public TextMeshProUGUI dropHintText;
    public TextMeshProUGUI notificationText;

    [Header("Boat Reference")]
    public BoatCollector boatCollector;

    [Header("Notification Settings")]
    public float notificationDuration = 2f;
    private float notificationTimer = 0f;

    public bool nearRecycleBin;

    void Update()
    {
        UpdateUI();
        UpdateNotification();
    }

    void UpdateUI()
    {
        if (boatCollector == null) return;

        // Update Inventory Text
        if (inventoryText != null)
        {
            inventoryText.text = $"Trash: <color=white>{boatCollector.trashInventory.Count}/{boatCollector.maxInventorySize}</color>";

            // Change color based on inventory status
            if (boatCollector.trashInventory.Count >= boatCollector.maxInventorySize)
            {
                inventoryText.color = Color.red;
            }
            else if (boatCollector.trashInventory.Count > 0)
            {
                inventoryText.color = Color.yellow;
            }
            else
            {
                inventoryText.color = Color.white;
            }
        }

        // Update Collect Prompt Text
        if (collectPromptText != null)
        {
            bool showCollectPrompt = boatCollector.nearbyCollectable != null &&
                                   !boatCollector.nearbyCollectable.IsCollected &&
                                   boatCollector.trashInventory.Count < boatCollector.maxInventorySize;

            collectPromptText.gameObject.SetActive(showCollectPrompt);

            if (showCollectPrompt)
            {
                collectPromptText.text = $"Press <color=yellow>E</color> to collect {boatCollector.nearbyCollectable.itemName}";
            }
        }

        // Update Drop Hint Text
        if (dropHintText != null)
        {
            bool showDropHint = boatCollector.nearRecycleBin && boatCollector.trashInventory.Count > 0;
            dropHintText.gameObject.SetActive(showDropHint);

            if (showDropHint)
            {
                dropHintText.text = $"Press <color=yellow>Q</color> to drop trash ({boatCollector.trashInventory.Count} items)";
            }
        }
    }

    void UpdateNotification()
    {
        if (notificationText != null && notificationText.gameObject.activeInHierarchy)
        {
            notificationTimer -= Time.deltaTime;
            if (notificationTimer <= 0f)
            {
                notificationText.gameObject.SetActive(false);
            }
        }
    }

    // Public method to show notifications
    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationText.gameObject.SetActive(true);
            notificationTimer = notificationDuration;
        }
    }

    // You can call this from other scripts to show notifications
    public void ShowCollectionNotification(string itemName)
    {
        ShowNotification($"<color=green>Collected {itemName}!</color>");
    }

    public void ShowDropNotification(string itemName)
    {
        ShowNotification($"<color=blue>Dropped {itemName} at recycle bin!</color>");
    }

    public void ShowInventoryFullNotification()
    {
        ShowNotification("<color=red>Inventory full! Drop trash first.</color>");
    }
}