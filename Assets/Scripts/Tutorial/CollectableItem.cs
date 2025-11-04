using UnityEngine;
using System.Collections;
using Photon.Pun;

public class CollectableItem : MonoBehaviourPunCallbacks
{
    [Header("Collectable Settings")]
    public string itemName = "Trash";
    public int scoreValue = 10;
    public float collectRange = 3f;

    [Header("Visual Feedback")]
    public Material highlightMaterial;
    public ParticleSystem collectParticles;
    public ParticleSystem dropParticles; // NEW: Particles for dropping

    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isPlayerInRange = false;
    private Vector3 originalScale; // NEW: Store original scale

    // MODIFIED: Proper backing field with public property
    private bool isCollected = false;
    public bool IsCollected
    {
        get { return isCollected; }
        set { isCollected = value; }
    }

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        // NEW: Store original scale
        originalScale = transform.localScale;

        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = collectRange;
        }

        // Add Collectable tag if not present
        if (!gameObject.CompareTag("Collectable"))
        {
            gameObject.tag = "Collectable";
        }
    }

    void Update()
    {
        if (!IsCollected && gameObject.activeInHierarchy)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !IsCollected)
        {
            PhotonView photonView = other.GetComponent<PhotonView>();
            if (photonView != null && !photonView.IsMine)
                return;

            isPlayerInRange = true;
            HighlightObject(true);

            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.SetNearbyCollectable(this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PhotonView photonView = other.GetComponent<PhotonView>();
            if (photonView != null && !photonView.IsMine)
                return;

            isPlayerInRange = false;
            HighlightObject(false);

            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.ClearNearbyCollectable(this);
            }
        }
    }

    // MODIFIED: Collection method
    public void Collect()
    {
        if (IsCollected) return;

        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        IsCollected = true;

        // Visual effects
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }

        // Notify game manager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            GameManager.Instance.AddCollectedItem(itemName);
        }

        // Clear from boat collector
        if (isPlayerInRange)
        {
            BoatCollector collector = FindObjectOfType<BoatCollector>();
            if (collector != null)
            {
                collector.ClearNearbyCollectable(this);
            }
            isPlayerInRange = false;
        }

        HighlightObject(false);

        // Hide the object (BoatCollector will handle showing it again when dropped)
        gameObject.SetActive(false);

        Debug.Log($"Collected: {itemName} (+{scoreValue} points)");
    }

    // NEW: Drop method for when trash is dropped at recycle bin
    public void DropAtRecycleBin(Vector3 dropPosition)
    {
        // Reset collected state
        IsCollected = false;
        isPlayerInRange = false;

        // Show the object
        gameObject.SetActive(true);

        // Set position at recycle bin
        transform.position = dropPosition;

        // Reset scale and rotation
        transform.localScale = originalScale;
        transform.rotation = Quaternion.identity;

        // Stop any highlight effects
        HighlightObject(false);
        StopPulsatingEffect();

        // Play drop particles if available
        if (dropParticles != null)
        {
            ParticleSystem particles = Instantiate(dropParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }

        // NEW: Optional - add some random rotation for visual variety
        StartCoroutine(ApplyRandomRotation());

        Debug.Log($"Dropped {itemName} at recycle bin");
    }

    // NEW: Coroutine for random rotation after dropping
    private IEnumerator ApplyRandomRotation()
    {
        float randomRotation = Random.Range(0f, 360f);
        transform.rotation = Quaternion.Euler(0, randomRotation, 0);
        yield return null;
    }

    // NEW: Reset method for item reuse
    public void ResetItem()
    {
        IsCollected = false;
        isPlayerInRange = false;
        HighlightObject(false);
        StopPulsatingEffect();
        transform.localScale = originalScale;
    }

    private void HighlightObject(bool highlight)
    {
        if (objectRenderer == null) return;

        if (highlight)
        {
            if (highlightMaterial != null)
            {
                objectRenderer.material = highlightMaterial;
            }
            StartPulsatingEffect();
        }
        else
        {
            if (originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }
            StopPulsatingEffect();
        }
    }

    private void StartPulsatingEffect()
    {
        if (!IsCollected)
        {
            StartCoroutine(PulsateEffect());
        }
    }

    private void StopPulsatingEffect()
    {
        StopAllCoroutines();
        if (transform != null)
        {
            transform.localScale = originalScale;
        }
    }

    private IEnumerator PulsateEffect()
    {
        float pulseSpeed = 2f;
        float pulseIntensity = 0.2f;

        while (isPlayerInRange && !IsCollected && gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            transform.localScale = originalScale * (1 + pulse);
            yield return null;
        }

        // Reset scale when done
        if (transform != null)
        {
            transform.localScale = originalScale;
        }
    }

    // NEW: Handle object states properly
    void OnEnable()
    {
        // Reset item when it's enabled again
        if (IsCollected)
        {
            ResetItem();
        }
    }

    void OnDisable()
    {
        // Clean up when disabled
        StopPulsatingEffect();
        isPlayerInRange = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
}

// Original Code//
/*using UnityEngine;
using System.Collections;
using Photon.Pun;

public class CollectableItem : MonoBehaviourPunCallbacks
{
    [Header("Collectable Settings")]
    public string itemName = "Trash";
    public int scoreValue = 10;
    public float collectRange = 3f;

    [Header("Visual Feedback")]
    public Material highlightMaterial;
    public ParticleSystem collectParticles;

    private Material originalMaterial;
    private Renderer objectRenderer;
    private bool isPlayerInRange = false;

    // Private field with public property
    private bool isCollected = false;
    public bool IsCollected => isCollected;

    void Start()
    {
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            originalMaterial = objectRenderer.material;
        }

        // Auto-add a trigger collider if none exists
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = collectRange;
        }
    }

    void Update()
    {
        // Rotate slowly for visual appeal
        if (!isCollected)
        {
            transform.Rotate(0, 30 * Time.deltaTime, 0);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isCollected)
        {
            // Only allow collection if this is the local player
            PhotonView photonView = other.GetComponent<PhotonView>();
            if (photonView != null && !photonView.IsMine)
                return;

            isPlayerInRange = true;
            HighlightObject(true);

            // Notify the boat that this item is collectable
            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.SetNearbyCollectable(this);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Only handle for local player
            PhotonView photonView = other.GetComponent<PhotonView>();
            if (photonView != null && !photonView.IsMine)
                return;

            isPlayerInRange = false;
            HighlightObject(false);

            // Notify the boat that no item is nearby
            BoatCollector collector = other.GetComponent<BoatCollector>();
            if (collector != null)
            {
                collector.ClearNearbyCollectable(this);
            }
        }
    }

    public void Collect()
    {
        if (isCollected) return;

        // In multiplayer, only allow the player who triggered to collect
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
            return;

        isCollected = true;

        // Visual effects
        if (collectParticles != null)
        {
            ParticleSystem particles = Instantiate(collectParticles, transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, 2f);
        }

        // Notify game manager or score system
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            GameManager.Instance.AddCollectedItem(itemName);
        }

        // Clear from boat collector before disabling
        if (isPlayerInRange)
        {
            BoatCollector collector = FindObjectOfType<BoatCollector>();
            if (collector != null)
            {
                collector.ClearNearbyCollectable(this);
            }
            isPlayerInRange = false;
        }

        // Remove highlight before disabling
        HighlightObject(false);

        // Handle object destruction differently in multiplayer
        if (PhotonNetwork.IsConnected)
        {
            // Use PhotonNetwork.Destroy for networked objects
            if (photonView != null)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            gameObject.SetActive(false);
        }

        Debug.Log($"Collected: {itemName} (+{scoreValue} points)");
    }

    private void HighlightObject(bool highlight)
    {
        if (objectRenderer == null) return;

        if (highlight)
        {
            // Apply highlight material
            if (highlightMaterial != null)
            {
                objectRenderer.material = highlightMaterial;
            }

            // Optional: Add additional highlight effects
            StartPulsatingEffect();
        }
        else
        {
            // Restore original material
            if (originalMaterial != null)
            {
                objectRenderer.material = originalMaterial;
            }

            // Stop any highlight effects
            StopPulsatingEffect();
        }
    }

    private void StartPulsatingEffect()
    {
        // Optional: Add a pulsating scale effect for better visibility
        StartCoroutine(PulsateEffect());
    }

    private void StopPulsatingEffect()
    {
        // Stop the pulsating effect and reset scale
        StopAllCoroutines();
        transform.localScale = Vector3.one;
    }

    private System.Collections.IEnumerator PulsateEffect()
    {
        float pulseSpeed = 2f;
        float pulseIntensity = 0.2f;
        Vector3 originalScale = transform.localScale;

        while (isPlayerInRange && !isCollected)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            transform.localScale = originalScale * (1 + pulse);
            yield return null;
        }

        // Reset scale when done
        transform.localScale = originalScale;
    }

    // Visualize the collect range in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, collectRange);
    }
}*/