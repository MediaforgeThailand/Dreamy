using UnityEngine;

namespace Dreamy.Extraction
{
    public sealed class ExtractionCraftingStation : MonoBehaviour
    {
        public bool TryCraft(ExtractionRecipeData recipe, ExtractionBaseStorage storage)
        {
            if (recipe == null || storage == null || !storage.Container.ConsumeItems(recipe.Inputs))
            {
                return false;
            }

            if (storage.Container.AddItems(recipe.Outputs))
            {
                return true;
            }

            storage.Container.AddItems(recipe.Inputs);
            return false;
        }
    }
}
