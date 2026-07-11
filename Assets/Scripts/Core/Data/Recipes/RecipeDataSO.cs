using UnityEngine;
using System.Collections.Generic;

namespace PrismIsland.Data
{
    [System.Serializable]
    public struct Ingredient
    {
        public ItemDataSO item;
        public int quantity;
    }

    [CreateAssetMenu(fileName = "NewRecipeData", menuName = "PrismIsland/Recipes/Recipe")]
    public class RecipeDataSO : ScriptableObject
    {
        public string id;
        public string recipeName;
        [TextArea]
        public string description;

        [Header("Output")]
        public ItemDataSO outputItem;
        public int outputQuantity = 1;

        [Header("Ingredients")]
        public List<Ingredient> ingredients = new List<Ingredient>();
    }
}
