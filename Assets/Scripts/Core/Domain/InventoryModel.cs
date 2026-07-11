using System;
using System.Collections.Generic;

namespace PrismIsland.Domain
{
    public class InventoryItem
    {
        public string Id { get; set; }
        public string ItemName { get; set; }
        public float Weight { get; set; }
        public int MaxStack { get; set; }
        public int Count { get; set; }
        public bool IsEquippable { get; set; }
    }

    public class InventoryModel
    {
        private List<InventoryItem> items = new List<InventoryItem>();
        public IReadOnlyList<InventoryItem> Items => items;

        public float MaxWeight { get; private set; }
        public float CurrentWeight { get; private set; }

        public event Action OnInventoryChanged;

        public void SetMaxWeight(float maxWeight)
        {
            MaxWeight = maxWeight;
            OnInventoryChanged?.Invoke();
        }

        public bool AddItem(InventoryItem itemData, int amount)
        {
            float totalWeightToAdd = itemData.Weight * amount;
            if (CurrentWeight + totalWeightToAdd > MaxWeight)
            {
                return false; // Cannot carry
            }

            // Find existing stack
            var existing = items.Find(i => i.Id == itemData.Id);
            if (existing != null)
            {
                existing.Count += amount;
                // Currently ignoring maxStack for simplicity in this model, 
                // but we can enforce it here if needed.
            }
            else
            {
                items.Add(new InventoryItem
                {
                    Id = itemData.Id,
                    ItemName = itemData.ItemName,
                    Weight = itemData.Weight,
                    MaxStack = itemData.MaxStack,
                    Count = amount,
                    IsEquippable = itemData.IsEquippable
                });
            }

            CurrentWeight += totalWeightToAdd;
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool RemoveItem(string itemId, int amount)
        {
            var existing = items.Find(i => i.Id == itemId);
            if (existing != null && existing.Count >= amount)
            {
                existing.Count -= amount;
                CurrentWeight -= (existing.Weight * amount);

                if (existing.Count <= 0)
                {
                    items.Remove(existing);
                }

                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetItemCount(string itemId)
        {
            var existing = items.Find(i => i.Id == itemId);
            return existing != null ? existing.Count : 0;
        }
        
        public void RecalculateWeight()
        {
            CurrentWeight = 0f;
            foreach (var item in items)
            {
                CurrentWeight += item.Weight * item.Count;
            }
            OnInventoryChanged?.Invoke();
        }
    }
}
