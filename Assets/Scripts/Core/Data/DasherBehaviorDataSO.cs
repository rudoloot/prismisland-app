using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewDasherData", menuName = "PrismIsland/Behavior/DasherData")]
    public class DasherBehaviorDataSO : EnemyBehaviorDataSO
    {
        public float dashRange = 6f;
        public float dashPrepTime = 0.5f;
        public float dashSpeed = 12f;
        public float dashDuration = 0.4f;
        public float postDashCooldown = 1f;
    }
}
