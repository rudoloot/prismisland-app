using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewArmorData", menuName = "PrismIsland/Items/Equipment/Armor")]
    public class ArmorDataSO : EquipmentDataSO
    {
        [Header("Armor Stats")]
        public float defense = 0f;
    }
}

