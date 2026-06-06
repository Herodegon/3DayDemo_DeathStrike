using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class EnemyEntry
{
    public string id;
    public GameObject prefab;
    [Min(0)] public int prewarmCount = 5;
    [Min(1)] public int cost = 1;
}

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance {get; private set;}
    
    [Header("Reference Settings")]
    [SerializeField] private EnemyEntry[] enemyEntries;
    [SerializeField] private Transform enemyTarget;
    [SerializeField] private Transform inactiveEnemyContainer;
    [SerializeField] private Transform activeEnemyContainer;
    [SerializeField] private bool allowPoolGrowth = true;

    [Header("Spawner Settings")]
    [SerializeField] private float spawnInterval = 5.0f;
    [SerializeField] private int maxEnemyPerWave = 10;
    [SerializeField] private int minEnemyCount = 0;
    [SerializeField] private int maxEnemyCount = 100;
    /// <summary>
    /// If true, the spawner will cap the number of enemies at the maxEnemyCount.
    /// </summary>
    [SerializeField] private bool capEnemyCount = true;
    [SerializeField] private int sumEnemyCost = 0;
    /// <summary>
    /// If true, the spawner will use a token system to evaluate the enemy cost and only spawn
    /// enough enemies to cover the cost. If false, the spawner will spawn as many enemies as possible.
    /// </summary>
    [SerializeField] private bool evaluateEnemyCost = false;

    private Coroutine spawnCoroutine;

    private readonly Dictionary<string, GameObject> enemyPrefabs = new();
    private readonly Dictionary<string, List<GameObject>> enemyPool = new();


    void Awake()
    {
        Instance = this;
        foreach (var entry in enemyEntries)
        {
            if (string.IsNullOrEmpty(entry.id) || entry.prefab == null) continue;
            enemyPrefabs[entry.id] = entry.prefab;
            enemyPool[entry.id] = new List<GameObject>();
            for (int i = 0; i < entry.prewarmCount; i++)
            {
                enemyPool[entry.id].Add(CreateInstance(entry.id, entry.prefab, i));
            }
        }
    }

    void Update()
    {
        if (spawnCoroutine != null) return;
        spawnCoroutine = StartCoroutine(SpawnAfterDelay(spawnInterval));
    }

    public void ReturnToPool(GameObject enemy)
    {
        string id = enemy.name.Split('_')[0];
        enemy.SetActive(false);
        enemy.transform.SetParent(inactiveEnemyContainer);
        enemyPool[id].Add(enemy);
    }

    private GameObject GetFromPool(string id, GameObject prefab)
    {
        var pool = enemyPool[id];
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].activeInHierarchy) return pool[i];
        }
        if (!allowPoolGrowth) return pool[0];
        var created = CreateInstance(id, prefab, pool.Count);
        pool.Add(created);
        return created;
    }

    private GameObject CreateInstance(string id, GameObject prefab, int index)
    {
        GameObject instance = Instantiate(prefab, inactiveEnemyContainer);
        instance.name = $"{id}_pooled{index}";
        instance.SetActive(false);
        instance.transform.SetParent(inactiveEnemyContainer);
        instance.GetComponent<EnemyAI>().ReturnToPool = true;
        return instance;
    }

    private void UpdateEnemyCount()
    {
        int currentEnemyCount = activeEnemyContainer.childCount;
        if (capEnemyCount && currentEnemyCount >= maxEnemyCount)
        {
            return;
        }
        int enemiesToSpawn = Mathf.Min(maxEnemyPerWave, maxEnemyCount - currentEnemyCount);
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            SpawnEnemy();
        }
    }

    #region Spawn Functions
    private void SpawnEnemy() 
    {
        EnemyEntry entry = enemyEntries[Random.Range(0, enemyEntries.Length)];
        SpawnEnemy(entry.id, entry.prefab);
    }
    private void SpawnEnemy(string id, GameObject prefab) 
    {
        GameObject enemy = GetFromPool(id, prefab);
        enemy.transform.SetParent(activeEnemyContainer);
        enemy.GetComponent<EnemyAI>().target = enemyTarget;
        enemy.SetActive(true);
    }
    #endregion

    #region Coroutines
    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        UpdateEnemyCount();
        spawnCoroutine = null;
    }
    #endregion
}
