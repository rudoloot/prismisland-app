using UnityEngine;
using System.Collections.Generic;
using PrismIsland.Data;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("기본 뼈대가 될 프리팹")]
    public GameObject enemyPrefab;
    public List<EnemyDataSO> spawnableEnemies = new List<EnemyDataSO>();
    public float spawnInterval = 2f;
    public float spawnRadius = 15f; 
    
    private Transform playerTransform;
    private float spawnTimer;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    void Update()
    {
        if (playerTransform == null || enemyPrefab == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            SpawnEnemy();
            spawnTimer = 0f;
        }
    }

    void SpawnEnemy()
    {
        Vector3 spawnPos = Vector3.zero;
        bool validPositionFound = false;

        // 적이 겹치지 않게 최대 10번 빈 공간 탐색
        for (int i = 0; i < 10; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized * spawnRadius * Random.Range(0.8f, 1.2f);
            spawnPos = playerTransform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);
            spawnPos.y = 1f;

            bool overlap = false;
            Collider[] cols = Physics.OverlapSphere(spawnPos, 1f);
            foreach (var c in cols) {
                if (c.GetComponent<Enemy>() != null) {
                    overlap = true;
                    break;
                }
            }

            // 해당 위치 반경 1m 이내에 다른 콜라이더(적)가 있는지 확인
            if (!overlap)
            {
                validPositionFound = true;
                break;
            }
        }

        // 10번 시도해도 빈 공간이 없다면 그냥 리턴 (스폰 스킵)
        if (!validPositionFound) return;

        GameObject enemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        Enemy enemyScript = enemyObj.GetComponent<Enemy>();
        if (enemyScript != null)
        {
            if (spawnableEnemies != null && spawnableEnemies.Count > 0)
            {
                EnemyDataSO randomData = spawnableEnemies[Random.Range(0, spawnableEnemies.Count)];
                enemyScript.Init(randomData);
            }
        }
    }
}
