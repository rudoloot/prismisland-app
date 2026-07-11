using UnityEngine;

namespace PrismIsland.Data
{
    public enum StructureType
    {
        Walkable,  // 플레이어/NPC가 밟고 지나갈 수 있음 (작업대, 침대 등)
        Obstacle   // 플레이어/NPC가 지나갈 수 없음 (벽, 상자 등)
    }

    [CreateAssetMenu(fileName = "NewStructureData", menuName = "PrismIsland/Items/Installable/Structure")]
    public class StructureDataSO : InstallableDataSO
    {
        [Header("Structure Data")]
        public StructureType structureType = StructureType.Walkable;
        public GameObject prefabToInstall;
    }
}

