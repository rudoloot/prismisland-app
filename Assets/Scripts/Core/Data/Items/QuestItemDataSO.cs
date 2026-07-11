using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewQuestItemData", menuName = "PrismIsland/Items/Quest Item")]
    public class QuestItemDataSO : ItemDataSO
    {
        [Header("Quest Data")]
        public string relatedQuestId;
        public bool canBeDropped = false;
        public bool canBeSold = false;
    }
}

