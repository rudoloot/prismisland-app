using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewEdibleData", menuName = "PrismIsland/Items/Consumable/Edible")]
    public class EdibleDataSO : ConsumableDataSO
    {
        [Header("Edible Stats")]
        public float healAmount = 0f;
    }
}

