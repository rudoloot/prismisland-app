using UnityEngine;
using System.Collections.Generic;
using PrismIsland.Data;

public class EnemyCamp : MonoBehaviour
{
    [Header("Camp Settings")]
    [Tooltip("기본 뼈대가 될 프리팹")]
    public GameObject enemyPrefab;
    public List<EnemyDataSO> spawnableEnemies = new List<EnemyDataSO>();
    public int numberOfEnemies = 5;
    public float spawnRadius = 5f;

    private List<GameObject> aliveEnemies = new List<GameObject>();
    private bool isCampCleared = false;
    private int clearedTimestamp = -1;

    void Start()
    {
        SpawnEnemies();
        
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNewHour += HandleNewHour;
        }
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNewHour -= HandleNewHour;
        }
    }

    void SpawnEnemies()
    {
        aliveEnemies.Clear();
        for (int i = 0; i < numberOfEnemies; i++)
        {
            Vector3 spawnPos = Vector3.zero;
            bool validPositionFound = false;

            for (int attempt = 0; attempt < 10; attempt++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
                spawnPos = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
                spawnPos.y = 1f;

                bool overlap = false;
                Collider[] cols = Physics.OverlapSphere(spawnPos, 1f);
                foreach (var c in cols) {
                    if (c.GetComponent<Enemy>() != null) {
                        overlap = true;
                        break;
                    }
                }

                if (!overlap)
                {
                    validPositionFound = true;
                    break;
                }
            }

            if (!validPositionFound) continue;
            if (enemyPrefab == null) continue;

            GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                if (spawnableEnemies != null && spawnableEnemies.Count > 0)
                {
                    EnemyDataSO randomData = spawnableEnemies[Random.Range(0, spawnableEnemies.Count)];
                    enemyScript.Init(randomData);
                }
                
                enemyObj.transform.SetParent(transform);
            }
            
            aliveEnemies.Add(enemyObj);
        }
        
        isCampCleared = false;
        clearedTimestamp = -1;
    }

    void Update()
    {
        if (isCampCleared) return;

        aliveEnemies.RemoveAll(item => item == null);

        if (aliveEnemies.Count == 0)
        {
            isCampCleared = true;
            if (TimeManager.Instance != null) {
                clearedTimestamp = TimeManager.Instance.currentDay * 24 + TimeManager.Instance.currentHour;
            }
            Debug.Log("Camp Cleared! Waiting 2 hours to respawn.");
        }
    }

    void HandleNewHour()
    {
        if (isCampCleared && TimeManager.Instance != null && clearedTimestamp != -1)
        {
            int currentTimestamp = TimeManager.Instance.currentDay * 24 + TimeManager.Instance.currentHour;
            if (currentTimestamp >= clearedTimestamp + 2)
            {
                Debug.Log("2 Hours passed! Respawning Camp!");
                SpawnEnemies();
            }
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, spawnRadius);
    }
}
