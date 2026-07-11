using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewAmmoData", menuName = "PrismIsland/Items/Consumable/Ammo")]
    public class AmmoDataSO : ConsumableDataSO
    {
        [Header("Ammo Stats")]
        public WeaponType compatibleWeaponType;
    }
}

