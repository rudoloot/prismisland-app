using UnityEngine;
using PrismIsland.Data;

public class ItemPickup : MonoBehaviour
{
    public ItemDataSO itemData;
    public int amount = 1;
    public float pickupRadius = 2f;
    private Transform player;
    private float cooldownTimer = 1.0f;

    void Start()
    {
        GameObject pObj = GameObject.Find("Player");
        if (pObj != null) player = pObj.transform;

        Renderer r = GetComponent<Renderer>();
        if (r != null && itemData != null) {
            if (r.material.HasProperty("_BaseColor"))
                r.material.SetColor("_BaseColor", itemData.fallbackColor);
            else
                r.material.color = itemData.fallbackColor;
        }
    }

    void Update()
    {
        if (cooldownTimer > 0) {
            cooldownTimer -= Time.deltaTime;
            return;
        }

        if (player == null || itemData == null) return;

        if (Vector3.Distance(transform.position, player.position) <= pickupRadius)
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.AddItem(itemData, amount);
                Destroy(gameObject);
            }
        }
    }
}
