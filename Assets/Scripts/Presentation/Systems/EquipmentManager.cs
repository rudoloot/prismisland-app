using UnityEngine;
using System.Collections.Generic;
using PrismIsland.Data;


public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance { get; private set; }

    public Dictionary<EquipSlot, ItemDataSO> equippedItems = new Dictionary<EquipSlot, ItemDataSO>();

    private GameObject rWeaponVisual;
    private GameObject lWeaponVisual;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        Transform rVis = transform.Find("VisualGun");
        if (rVis != null) {
            rWeaponVisual = rVis.gameObject;
        }

        // 초기 아이템 지급: 권총, 나무검, 총알 50발
        if (InventoryManager.Instance != null) {
            ItemDataSO gun = ItemDatabase.GetItem("BasicGun");
            ItemDataSO sword = ItemDatabase.GetItem("WoodenSword");
            ItemDataSO bullet = ItemDatabase.GetItem("Bullet");
            
            if (gun != null) InventoryManager.Instance.AddItem(gun, 1);
            if (sword != null) InventoryManager.Instance.AddItem(sword, 1);
            if (bullet != null) InventoryManager.Instance.AddItem(bullet, 50);
        }

        UpdateVisuals();
    }

    public void UpdateVisuals()
    {
        WeaponController wc = GetComponent<WeaponController>();

        UpdateWeaponVisual(EquipSlot.R_Weapon, ref rWeaponVisual, 0.6f);
        UpdateWeaponVisual(EquipSlot.L_Weapon, ref lWeaponVisual, -0.6f);

        if (wc != null) {
            ItemDataSO rWeapon = GetItemInSlot(EquipSlot.R_Weapon);
            ItemDataSO lWeapon = GetItemInSlot(EquipSlot.L_Weapon);
            wc.EquipWeapons(rWeapon as WeaponDataSO, lWeapon as WeaponDataSO);
        }
    }

    private void UpdateWeaponVisual(EquipSlot slot, ref GameObject visualObj, float xOffset)
    {
        ItemDataSO baseItem = GetItemInSlot(slot);
        WeaponDataSO item = baseItem as WeaponDataSO;
        
        if (item == null || item.weaponType != WeaponType.OneHanded) {
            if (visualObj != null) visualObj.SetActive(false);
            return;
        }
        
        if (visualObj == null) {
            visualObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualObj.name = slot.ToString() + "_Visual";
            visualObj.transform.SetParent(transform);
            Destroy(visualObj.GetComponent<Collider>());
        }
        
        visualObj.SetActive(true);
        Renderer r = visualObj.GetComponent<Renderer>();
        if (r != null) {
            Material mat = r.material; 
            if (mat != null) {
                if (item.id == "BasicGun") {
                    visualObj.transform.localScale = new Vector3(0.2f, 0.2f, 0.8f);
                    visualObj.transform.localRotation = Quaternion.identity; // 총은 앞을 향함
                    visualObj.transform.localPosition = new Vector3(xOffset, 0.2f, 0.6f);
                    mat.color = item.fallbackColor;
                } else if (item.id == "WoodenSword") {
                    visualObj.transform.localScale = new Vector3(0.1f, 1.4f, 0.1f); // 검은 위로 길게 (막대기 형태)
                    visualObj.transform.localRotation = Quaternion.Euler(45f, 0f, 0f); // 약간 앞으로 기울여서 잡음
                    visualObj.transform.localPosition = new Vector3(xOffset, 0.5f, 0.6f);
                    mat.color = item.fallbackColor;
                }
            }
        }
        
        // 총구(FirePoint) 위치 조정
        if (item.id == "BasicGun") {
            Transform fpTrans = visualObj.transform.Find("FirePoint");
            if (fpTrans == null) {
                GameObject fp = new GameObject("FirePoint");
                fp.transform.SetParent(visualObj.transform);
                fp.transform.localPosition = new Vector3(0, 0, 0.5f);
            }
        }
    }

    public string GetItemNameInSlot(EquipSlot slot)
    {
        if (equippedItems.ContainsKey(slot)) return equippedItems[slot].itemName;
        return "";
    }

    public ItemDataSO GetItemInSlot(EquipSlot slot)
    {
        if (equippedItems.ContainsKey(slot)) return equippedItems[slot];
        return null;
    }

    public void EquipItem(ItemDataSO item)
    {
        if (item == null) return;

        EquipmentDataSO equipData = item as EquipmentDataSO;
        if (equipData == null) return; // 장비가 아니면 장착 불가

        EquipSlot targetSlot;
        
        WeaponDataSO weaponData = item as WeaponDataSO;
        if (weaponData != null && weaponData.weaponType == WeaponType.OneHanded) {
            if (!equippedItems.ContainsKey(EquipSlot.R_Weapon)) {
                targetSlot = EquipSlot.R_Weapon;
            } else if (!equippedItems.ContainsKey(EquipSlot.L_Weapon)) {
                targetSlot = EquipSlot.L_Weapon;
            } else {
                targetSlot = EquipSlot.R_Weapon;
            }
        } else {
            targetSlot = equipData.equipSlot;
        }

        if (equippedItems.ContainsKey(targetSlot))
        {
            UnequipItem(targetSlot);
        }

        equippedItems[targetSlot] = item;
        if (InventoryManager.Instance != null) {
            InventoryManager.Instance.RemoveItem(item, 1);
        }

        UpdateVisuals();
    }

    public void UnequipItem(EquipSlot slot)
    {
        if (equippedItems.ContainsKey(slot))
        {
            ItemDataSO item = equippedItems[slot];
            equippedItems.Remove(slot);
            if (InventoryManager.Instance != null) {
                InventoryManager.Instance.AddItem(item, 1);
            }
            UpdateVisuals();
        }
    }

    public GameObject GetWeaponVisual(EquipSlot slot)
    {
        if (slot == EquipSlot.R_Weapon) return rWeaponVisual;
        if (slot == EquipSlot.L_Weapon) return lWeaponVisual;
        return null;
    }
}
