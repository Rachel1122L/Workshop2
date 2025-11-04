using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FloatingItem : MonoBehaviour
{
    [Header("Float Settings")]
    public float waterHeight = 0f;
    public float bobFrequency = 1.5f;
    public float bobAmplitude = 0.15f;
    public float floatingSpeed = 0.5f; // Now controls vertical floating speed

    [Header("Collection")]
    public float collectionRadius = 1f;
    public string boatTag = "Boat"; // Set your boat tag in inspector

    private Vector3 startPosition;
    private float bobOffset;
    private bool isCollected = false;
    private float floatTimer = 0f;

    void Start()
    {
        startPosition = transform.position;
        bobOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (isCollected)
            return;

        // Apply bobbing motion with vertical floating
        float bob = Mathf.Sin(Time.time * bobFrequency + bobOffset) * bobAmplitude;
        floatTimer += Time.deltaTime * floatingSpeed;

        Vector3 pos = startPosition;
        pos.y = waterHeight + bob + Mathf.Sin(floatTimer) * 0.1f; // Added gentle vertical floating
        transform.position = pos;

        // Removed rotation - items no longer rotate
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(boatTag) && !isCollected)
        {
            CollectItem(other.gameObject);
        }
    }

    void CollectItem(GameObject boat)
    {
        isCollected = true;
        // Add your collection logic here
        // Examples:
        // - Add to inventory
        // - Play sound effect
        // - Play particle effect
        // - Increment score

        Debug.Log("Item collected by boat!");
        Destroy(gameObject);
    }

    // Optional: Visualize collection radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}