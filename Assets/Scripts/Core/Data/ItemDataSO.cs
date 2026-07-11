using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "PrismIsland/Items/Base Item")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("General")]
        public string id; // 고유 ID
        public string itemName; // UI 표시 이름
        public float weight;
        public Sprite icon; // 2D 아이콘
        public Color fallbackColor = Color.white; // 아이콘이 없을 때 대체 색상

        [Header("Economy")]
        public int price;

        [Header("Combat Stats")]
        public float attackPower;
        public float attackRange;
        public float defense;

        [Header("Extra")]
        [TextArea]
        public string gimmickDescription;
    }
}
