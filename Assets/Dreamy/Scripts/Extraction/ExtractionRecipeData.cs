using System.Collections.Generic;
using UnityEngine;

namespace Dreamy.Extraction
{
    [CreateAssetMenu(menuName = "Dreamy/Extraction/Recipe", fileName = "Recipe")]
    public sealed class ExtractionRecipeData : ScriptableObject
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private List<ExtractionItemStack> inputs = new List<ExtractionItemStack>();
        [SerializeField] private List<ExtractionItemStack> outputs = new List<ExtractionItemStack>();
        [SerializeField] private int weaponRepairAmount;

        public string RecipeId => string.IsNullOrWhiteSpace(recipeId) ? name : recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public IReadOnlyList<ExtractionItemStack> Inputs => inputs;
        public IReadOnlyList<ExtractionItemStack> Outputs => outputs;
        public int WeaponRepairAmount => weaponRepairAmount;

        private void OnValidate()
        {
            inputs.RemoveAll(item => item == null || !item.IsValid);
            outputs.RemoveAll(item => item == null || !item.IsValid);
            weaponRepairAmount = Mathf.Max(0, weaponRepairAmount);
        }
    }
}
