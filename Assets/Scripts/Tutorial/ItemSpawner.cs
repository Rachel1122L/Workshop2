using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class ItemSpawner : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public class TrashItem
    {
        public GameObject prefab;
        [Range(0, 100)]
        public int spawnWeight = 50;
    }

    [Header("Spawn Settings")]
    [SerializeField] private TrashItem[] trashPrefabs;
    [SerializeField] private int maxConcurrentItems = 10;
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float itemLifetime = 30f;

    [Header("Network Settings")]
    [SerializeField] private bool networkEnabled = true;

    [Header("Spawn Area")]
    [SerializeField] private Vector3 spawnArea = new Vector3(10f, 0f, 10f);
    [SerializeField] private float waterHeight = 0f;
    [SerializeField] private LayerMask spawnCheckMask = 1;
    [SerializeField] private float spawnCheckRadius = 0.1f;

    [Header("Terrain Exclusion")]
    [SerializeField] private Terrain terrain;

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private bool showGizmos = true;

    private List<GameObject> activeTrashItems = new List<GameObject>();
    private Coroutine spawnCoroutine;
    private int totalSpawnWeight;

    void Start()
    {
        if (debugMode) Debug.Log("ItemSpawner Started");

        CalculateTotalWeight();

        // Only master client handles spawning in networked games
        if (!networkEnabled || PhotonNetwork.IsMasterClient)
        {
            if (spawnOnStart)
                StartSpawning();
        }
    }

    void CalculateTotalWeight()
    {
        totalSpawnWeight = 0;
        foreach (var trash in trashPrefabs)
            totalSpawnWeight += trash.spawnWeight;
    }

    public void StartSpawning()
    {
        if (spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (activeTrashItems.Count < maxConcurrentItems)
                TrySpawnTrash();
        }
    }

    public void TrySpawnTrash()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector3 spawnPosition = GetRandomSpawnPosition();

            if (!IsValidWaterPosition(spawnPosition))
                continue;

            if (IsValidSpawnPosition(spawnPosition))
            {
                TrashItem trashToSpawn = GetRandomTrashItem();
                if (trashToSpawn != null && trashToSpawn.prefab != null)
                {
                    SpawnNetworkItem(trashToSpawn.prefab, spawnPosition);
                    return;
                }
            }
        }
    }

    private void SpawnNetworkItem(GameObject prefab, Vector3 position)
    {
        if (networkEnabled && PhotonNetwork.IsConnected)
        {
            // Network instantiation
            GameObject newTrash = PhotonNetwork.Instantiate(
                prefab.name,
                position,
                GetRandomRotation()
            );

            activeTrashItems.Add(newTrash);
            StartCoroutine(DestroyAfterTime(newTrash, itemLifetime));
        }
        else
        {
            // Local instantiation for offline mode
            GameObject newTrash = Instantiate(prefab, position, GetRandomRotation());
            activeTrashItems.Add(newTrash);
            StartCoroutine(DestroyAfterTime(newTrash, itemLifetime));
        }
    }

    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 randomPoint = new Vector3(
            Random.Range(-spawnArea.x / 2, spawnArea.x / 2),
            waterHeight,
            Random.Range(-spawnArea.z / 2, spawnArea.z / 2)
        );
        return transform.position + randomPoint;
    }

    private bool IsValidSpawnPosition(Vector3 position)
    {
        return !Physics.CheckSphere(position, spawnCheckRadius, spawnCheckMask);
    }

    private bool IsValidWaterPosition(Vector3 position)
    {
        if (terrain == null) return true;
        float terrainHeight = terrain.SampleHeight(position) + terrain.GetPosition().y;
        return terrainHeight < waterHeight;
    }

    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(0, Random.Range(0, 360f), 0);
    }

    private TrashItem GetRandomTrashItem()
    {
        if (trashPrefabs.Length == 0) return null;
        if (totalSpawnWeight <= 0) return trashPrefabs[Random.Range(0, trashPrefabs.Length)];

        int randomValue = Random.Range(0, totalSpawnWeight);
        int currentWeight = 0;

        foreach (var trash in trashPrefabs)
        {
            currentWeight += trash.spawnWeight;
            if (randomValue < currentWeight)
                return trash;
        }

        return trashPrefabs[0];
    }

    private IEnumerator DestroyAfterTime(GameObject trashObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (trashObject != null)
        {
            activeTrashItems.Remove(trashObject);

            if (networkEnabled && PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Destroy(trashObject);
            }
            else
            {
                Destroy(trashObject);
            }
        }
    }

    // Master client management
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (debugMode) Debug.Log("Master client switched");

        // Stop spawning if we're no longer master client
        if (spawnCoroutine != null && !PhotonNetwork.IsMasterClient)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        // Start spawning if we became master client
        else if (spawnCoroutine == null && PhotonNetwork.IsMasterClient)
        {
            StartSpawning();
        }
    }
}