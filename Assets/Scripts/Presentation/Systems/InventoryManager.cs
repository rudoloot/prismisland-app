using UnityEngine;
using PrismIsland.Data;
using PrismIsland.Domain;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public InventoryModel Model { get; private set; }

    // Backward compatibility for easy UI access
    public float TotalWeight => Model.CurrentWeight;
    public float MaxWeight => Model.MaxWeight;

    // Optional: Keep a dict around for fast UI retrieval of SOs if needed,
    // but the Model is the source of truth.

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        Model = new InventoryModel();
    }

    void Start()
    {
        if (PlayerStats.Instance != null && PlayerStats.Instance.Model != null)
        {
            Model.SetMaxWeight(PlayerStats.Instance.Model.Strength * 10f);
        }

        // 초기 아이템 지급은 EquipmentManager.cs에서 담당합니다.
    }

    void Update()
    {
        // Update MaxWeight continuously based on PlayerStatsModel
        if (PlayerStats.Instance != null && PlayerStats.Instance.Model != null)
        {
            Model.SetMaxWeight(PlayerStats.Instance.Model.Strength * 10f);
        }
    }

    public void AddItem(ItemDataSO type, int amount = 1)
    {
        if (type == null) return;

        var invItem = new InventoryItem
        {
            Id = type.id,
            ItemName = type.itemName,
            Weight = type.weight,
            MaxStack = 99,
            IsEquippable = type is EquipmentDataSO
        };

        bool success = Model.AddItem(invItem, amount);
        if (success)
        {
            Debug.Log($"Picked up {amount} {type.itemName}. Total weight: {TotalWeight} / {MaxWeight}");
        }
        else
        {
            Debug.LogWarning($"Inventory full! Could not pick up {type.itemName}");
        }
    }

    public void RemoveItem(ItemDataSO type, int amount = 1)
    {
        if (type == null) return;
        Model.RemoveItem(type.id, amount);
    }

    public int GetItemCount(ItemDataSO type)
    {
        if (type == null) return 0;
        return Model.GetItemCount(type.id);
    }

    public void DropItem(ItemDataSO type)
    {
        if (type == null) return;
        if (GetItemCount(type) <= 0) return;
        
        RemoveItem(type, 1);

        GameObject prefab = Resources.Load<GameObject>("IronPickup"); // 임시 범용 프리팹 사용
        if (prefab != null && PlayerStats.Instance != null) {
            Vector3 dropPos = PlayerStats.Instance.transform.position + new Vector3(Random.Range(-1.5f, 1.5f), 0.5f, Random.Range(-1.5f, 1.5f));
            GameObject dropObj = Instantiate(prefab, dropPos, Quaternion.identity);
            ItemPickup ip = dropObj.GetComponent<ItemPickup>();
            if (ip != null) {
                ip.itemData = type;
                ip.amount = 1;
            }
            Renderer r = dropObj.GetComponent<Renderer>();
            if (r != null) {
                if (r.material.HasProperty("_BaseColor"))
                    r.material.SetColor("_BaseColor", type.fallbackColor);
                else
                    r.material.color = type.fallbackColor;
            }
        }
    }
}
