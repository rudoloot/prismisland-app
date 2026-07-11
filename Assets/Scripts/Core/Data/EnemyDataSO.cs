using UnityEngine;
using System.Collections.Generic;

namespace PrismIsland.Data
{
    [System.Serializable]
    public class DropItemInfo
    {
        public ItemDataSO item;
        public float dropChance; // 0.0f ~ 1.0f
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [System.Serializable]
    public class EnemyAttackConfig
    {
        public EnemyBehaviorDataSO behaviorData;
        public float damage = 10f;
        public float attackRange = 2f;
        public float attackRate = 1f;
    }

    public enum EnemyBehavior { Dasher, Ranged }

    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "PrismIsland/EnemyData")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("General")]
        public string enemyName;
        public float maxHealth = 30f;
        public float moveSpeed = 3f;
        
        [Header("Visual")]
        public Color enemyColor = Color.white;
        public float scale = 1f;

        [Header("Behavior")]
        [Tooltip("True if the enemy attacks on sight")]
        public bool isAggressive = true;
        public float detectionRange = 10f;
        
        [Header("Attacks")]
        public float globalAttackCooldown = 1f;
        public List<EnemyAttackConfig> attacks;
        
        [Header("Drops")]
        public List<DropItemInfo> dropTable;
    }
}
