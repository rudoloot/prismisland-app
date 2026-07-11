using UnityEngine;

namespace PrismIsland.Data
{
    public abstract class EquipmentDataSO : ItemDataSO
    {
        [Header("Equipment Stats")]
        public EquipSlot equipSlot;
    }
}

