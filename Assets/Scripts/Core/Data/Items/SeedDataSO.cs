using UnityEngine;

namespace PrismIsland.Data
{
    [CreateAssetMenu(fileName = "NewSeedData", menuName = "PrismIsland/Items/Installable/Seed")]
    public class SeedDataSO : InstallableDataSO
    {
        [Header("Seed Data")]
        public string cropType;
        public float growTime;
    }
}

