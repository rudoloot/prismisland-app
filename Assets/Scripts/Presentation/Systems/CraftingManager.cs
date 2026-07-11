using UnityEngine;
using System.Collections.Generic;
using PrismIsland.Data;

public class CraftingManager : MonoBehaviour
{
    public static CraftingManager Instance { get; private set; }

    public List<RecipeDataSO> availableRecipes = new List<RecipeDataSO>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadRecipes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadRecipes()
    {
        RecipeDataSO[] loadedRecipes = Resources.LoadAll<RecipeDataSO>("Recipes");
        availableRecipes.AddRange(loadedRecipes);
        Debug.Log($"Loaded {availableRecipes.Count} recipes.");
    }

    public bool CanCraft(RecipeDataSO recipe, int quantityToCraft = 1)
    {
        if (recipe == null || InventoryManager.Instance == null) return false;

        foreach (var ingredient in recipe.ingredients)
        {
            int requiredAmount = ingredient.quantity * quantityToCraft;
            int currentAmount = InventoryManager.Instance.GetItemCount(ingredient.item);
            if (currentAmount < requiredAmount)
            {
                return false;
            }
        }

        return true;
    }

    public void CraftItem(RecipeDataSO recipe, int quantityToCraft = 1)
    {
        if (!CanCraft(recipe, quantityToCraft)) return;

        // Consume ingredients
        foreach (var ingredient in recipe.ingredients)
        {
            int requiredAmount = ingredient.quantity * quantityToCraft;
            InventoryManager.Instance.RemoveItem(ingredient.item, requiredAmount);
        }

        // Add output item
        InventoryManager.Instance.AddItem(recipe.outputItem, recipe.outputQuantity * quantityToCraft);
        Debug.Log($"Crafted {recipe.outputQuantity * quantityToCraft}x {recipe.outputItem.itemName}");
    }
}
