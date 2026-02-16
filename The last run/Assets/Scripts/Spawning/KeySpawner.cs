using System.Collections.Generic;
using UnityEngine;

public class KeySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject keyPrefab;
    [SerializeField] private Transform spawnPointsParent;
    [SerializeField] private Transform spawnedParent;

    [Header("Settings")]
    [SerializeField] private int keysToSpawn = 3;

    void Start()
    {
        SpawnKeys();
    }

    void SpawnKeys()
    {
        if (keyPrefab == null)
        {
            Debug.LogWarning("KeySpawner: keyPrefab is not assigned.");
            return;
        }

        if (spawnPointsParent == null)
        {
            Debug.LogWarning("KeySpawner: spawnPointsParent is not assigned.");
            return;
        }

        Transform[] spawnPoints = spawnPointsParent.GetComponentsInChildren<Transform>();
        if (spawnPoints.Length <= 1)
        {
            Debug.LogWarning("KeySpawner: spawnPointsParent has no children.");
            return;
        }

        List<Transform> available = new List<Transform>();
        for (int i = 1; i < spawnPoints.Length; i++)
        {
            available.Add(spawnPoints[i]);
        }

        int count = Mathf.Min(keysToSpawn, available.Count);
        for (int i = 0; i < count; i++)
        {
            int pickIndex = Random.Range(0, available.Count);
            Transform point = available[pickIndex];
            available.RemoveAt(pickIndex);

            GameObject spawnedKey = Instantiate(keyPrefab, point.position, point.rotation, spawnedParent);
            spawnedKey.tag = "Gatherable";
        }
    }
}
