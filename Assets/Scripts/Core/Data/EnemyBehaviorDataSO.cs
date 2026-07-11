using UnityEngine;

namespace PrismIsland.Data
{
    public abstract class EnemyBehaviorDataSO : ScriptableObject
    {
        public string animationTriggerName = "Attack";
        public float hitboxSize = 1f;
    }
}
