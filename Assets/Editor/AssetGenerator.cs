using UnityEditor;
using UnityEngine;
using PrismIsland.Data;

public class AssetGenerator {
    [MenuItem("PrismIsland/Generate Items")]
    public static void Generate() {
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Items")) AssetDatabase.CreateFolder("Assets/Resources", "Items");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Recipes")) AssetDatabase.CreateFolder("Assets/Resources", "Recipes");

        // --- 무기 (Weapons) ---
        CreateWeapon("BasicGun", "권총", 2.0f, EquipSlot.R_Weapon, WeaponType.OneHanded, CombatType.Ranged, 10f, 15f, 2f, 12, Color.black);
        CreateWeapon("WoodenSword", "Wooden Sword", 3.0f, EquipSlot.R_Weapon, WeaponType.OneHanded, CombatType.Melee, 15f, 2.5f, 1.5f, 0, new Color(0.8f, 0.6f, 0.4f));
        CreateWeapon("Dagger", "단검", 1.0f, EquipSlot.R_Weapon, WeaponType.OneHanded, CombatType.Melee, 8f, 1.5f, 3f, 0, Color.gray);
        CreateWeapon("Axe", "도끼", 4.0f, EquipSlot.R_Weapon, WeaponType.OneHanded, CombatType.Melee, 25f, 2.0f, 0.8f, 0, new Color(0.5f, 0.5f, 0.5f));
        CreateWeapon("Pickaxe", "곡괭이", 3.5f, EquipSlot.R_Weapon, WeaponType.OneHanded, CombatType.Melee, 18f, 2.0f, 1.0f, 0, new Color(0.6f, 0.6f, 0.6f));

        // --- 방어구 (Armor) ---
        CreateArmor("Sunglasses", "선그라스", 0.1f, EquipSlot.Head, 1f, Color.black);
        CreateArmor("MilitaryHelmet", "군용철모", 2.5f, EquipSlot.Head, 15f, new Color(0.2f, 0.3f, 0.2f));
        
        CreateArmor("LeatherArmor", "가죽갑옷", 5.0f, EquipSlot.Chest, 10f, new Color(0.6f, 0.4f, 0.2f));
        CreateArmor("IronArmor", "철갑옷", 15.0f, EquipSlot.Chest, 30f, Color.gray);

        CreateArmor("LeatherPants", "가죽하의", 3.0f, EquipSlot.Legs, 7f, new Color(0.6f, 0.4f, 0.2f));
        CreateArmor("IronPants", "철하의", 10.0f, EquipSlot.Legs, 20f, Color.gray);

        CreateArmor("LeatherGloves", "가죽장갑", 0.5f, EquipSlot.Gloves, 3f, new Color(0.6f, 0.4f, 0.2f));
        CreateArmor("IronGloves", "철장갑", 2.0f, EquipSlot.Gloves, 8f, Color.gray);

        CreateArmor("LeatherShoes", "가죽신발", 1.0f, EquipSlot.Shoes, 4f, new Color(0.6f, 0.4f, 0.2f));
        CreateArmor("IronShoes", "철신발", 3.0f, EquipSlot.Shoes, 10f, Color.gray);

        CreateArmor("WoodNecklace", "나무 목걸이", 0.1f, EquipSlot.Accessory, 2f, new Color(0.8f, 0.6f, 0.4f));
        CreateArmor("LeatherNecklace", "가죽 목걸이", 0.2f, EquipSlot.Accessory, 5f, new Color(0.6f, 0.4f, 0.2f));

        // --- 소모품 (Consumable) ---
        CreateEdible("HealthPotion", "회복약", 0.5f, 30f, Color.green);
        CreateAmmo("Bullet", "총알", 0.01f, WeaponType.OneHanded, Color.yellow);

        // --- 재료 (Material) ---
        CreateMaterial("Iron", "철", 0.5f, Color.gray);
        CreateMaterial("Wood", "나무", 1.0f, new Color(0.5f, 0.3f, 0.1f));
        CreateMaterial("Paper", "종이", 0.05f, Color.white);
        CreateMaterial("Stone", "돌", 2.0f, new Color(0.4f, 0.4f, 0.4f));
        CreateMaterial("Leather", "가죽", 0.3f, new Color(0.6f, 0.4f, 0.2f));
        CreateMaterial("Fiber", "섬유", 0.1f, new Color(0.8f, 0.8f, 0.8f));
        CreateMaterial("Meat", "고기", 0.5f, Color.red);

        // --- 퀘스트 (Quest) ---
        CreateQuestItem("Contract", "계약서", 0.01f, "Q001", Color.white);

        // --- 설치 아이템 (Installable) ---
        CreateSeed("WoodSeed", "나무 묘목", 0.2f, "Tree", 300f, Color.green);
        CreateStructure("Box", "상자", 5.0f, null, new Color(0.5f, 0.3f, 0.1f), StructureType.Obstacle);
        CreateStructure("Workbench", "작업대", 10.0f, null, new Color(0.6f, 0.4f, 0.2f), StructureType.Walkable);

        // --- 레시피 (Recipes) ---
        CreateRecipe("Recipe_Box", "상자 제작", "기본적인 아이템 보관함", "Box", 1, new string[] { "Wood" }, new int[] { 5 });
        CreateRecipe("Recipe_Workbench", "작업대 제작", "더 많은 아이템을 제작할 수 있는 작업대", "Workbench", 1, new string[] { "Wood", "Iron" }, new int[] { 5, 5 });
        CreateRecipe("Recipe_Axe", "도끼 제작", "나무를 캘 수 있는 도구", "Axe", 1, new string[] { "Wood", "Iron" }, new int[] { 1, 3 });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Items and Recipes generated successfully!");
    }

    private static T CreateAsset<T>(string id) where T : ScriptableObject {
        string path = $"Assets/Resources/Items/{id}.asset";
        T item = AssetDatabase.LoadAssetAtPath<T>(path);
        if (item == null) {
            item = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(item, path);
        }
        return item;
    }

    private static void CreateWeapon(string id, string name, float weight, EquipSlot slot, WeaponType wType, CombatType cType, float damage, float range, float rate, int ammo, Color color) {
        var item = CreateAsset<WeaponDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.equipSlot = slot; item.weaponType = wType; item.combatType = cType;
        item.damage = damage; item.attackRange = range; item.attackRate = rate; item.maxAmmo = ammo;
        EditorUtility.SetDirty(item);
    }

    private static void CreateArmor(string id, string name, float weight, EquipSlot slot, float defense, Color color) {
        var item = CreateAsset<ArmorDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.equipSlot = slot; item.defense = defense;
        EditorUtility.SetDirty(item);
    }

    private static void CreateEdible(string id, string name, float weight, float heal, Color color) {
        var item = CreateAsset<EdibleDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.healAmount = heal;
        EditorUtility.SetDirty(item);
    }

    private static void CreateAmmo(string id, string name, float weight, WeaponType wType, Color color) {
        var item = CreateAsset<AmmoDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.compatibleWeaponType = wType;
        EditorUtility.SetDirty(item);
    }

    private static void CreateMaterial(string id, string name, float weight, Color color) {
        var item = CreateAsset<MaterialDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        EditorUtility.SetDirty(item);
    }

    private static void CreateQuestItem(string id, string name, float weight, string qId, Color color) {
        var item = CreateAsset<QuestItemDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.relatedQuestId = qId; item.canBeSold = false; item.canBeDropped = false;
        EditorUtility.SetDirty(item);
    }

    private static void CreateSeed(string id, string name, float weight, string cType, float gTime, Color color) {
        var item = CreateAsset<SeedDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.cropType = cType; item.growTime = gTime;
        EditorUtility.SetDirty(item);
    }

    private static void CreateStructure(string id, string name, float weight, GameObject prefab, Color color, StructureType type = StructureType.Walkable) {
        var item = CreateAsset<StructureDataSO>(id);
        item.id = id; item.itemName = name; item.weight = weight; item.fallbackColor = color;
        item.prefabToInstall = prefab;
        item.structureType = type;
        EditorUtility.SetDirty(item);
    }

    private static void CreateRecipe(string id, string name, string desc, string outItemId, int outQty, string[] inItemIds, int[] inQtys) {
        string path = $"Assets/Resources/Recipes/{id}.asset";
        RecipeDataSO recipe = AssetDatabase.LoadAssetAtPath<RecipeDataSO>(path);
        if (recipe == null) {
            recipe = ScriptableObject.CreateInstance<RecipeDataSO>();
            AssetDatabase.CreateAsset(recipe, path);
        }
        
        recipe.id = id;
        recipe.recipeName = name;
        recipe.description = desc;
        recipe.outputItem = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"Assets/Resources/Items/{outItemId}.asset");
        recipe.outputQuantity = outQty;
        
        recipe.ingredients.Clear();
        for (int i = 0; i < inItemIds.Length; i++) {
            recipe.ingredients.Add(new Ingredient {
                item = AssetDatabase.LoadAssetAtPath<ItemDataSO>($"Assets/Resources/Items/{inItemIds[i]}.asset"),
                quantity = inQtys[i]
            });
        }
        EditorUtility.SetDirty(recipe);
    }
}
