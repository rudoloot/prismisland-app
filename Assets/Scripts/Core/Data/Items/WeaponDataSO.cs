using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "PrismIsland/Items/Equipment/Weapon")]
    public class WeaponDataSO : EquipmentDataSO
    {
        [Header("Weapon Stats")]
        public WeaponType weaponType;
        public CombatType combatType = CombatType.None;
        public float damage = 0f;
        public float attackRange = 0f;
        [Tooltip("Melee: Sweep angle (휘두르는 부채꼴 각도) / Ranged: Spread angle (탄퍼짐 각도)")]
        public float attackAngle = 90f;
        [Tooltip("Attacks per second (초당 공격 횟수)")]
        public float attackRate = 1f;
        public int maxAmmo = 0;
    }
}

