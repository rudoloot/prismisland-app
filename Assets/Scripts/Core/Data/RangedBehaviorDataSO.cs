using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewRangedData", menuName = "PrismIsland/Behavior/RangedData")]
    public class RangedBehaviorDataSO : EnemyBehaviorDataSO
    {
        public float shootRange = 8f;
        public float shootPrepTime = 0.5f;
        public float retreatTime = 1f;
        public float retreatSpeed = 4f;
        public GameObject projectilePrefab;
    }
}
