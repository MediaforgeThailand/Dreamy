using UnityEngine;

namespace Dreamy.Extraction
{
    public enum ExtractionItemCategory
    {
        Generic,
        Weapon,
        Material,
        Consumable,
        Quest,
        MapUnlock,
        Crop,
        Currency
    }

    [CreateAssetMenu(menuName = "Dreamy/Extraction/Item Data", fileName = "ItemData")]
    public sealed class ExtractionItemData : ScriptableObject
    {
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;
        [SerializeField] private ExtractionItemCategory category = ExtractionItemCategory.Generic;
        [SerializeField] private Sprite icon;
        [SerializeField] private int maxStack = 99;

        public string ItemId => string.IsNullOrWhiteSpace(itemId) ? name : itemId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public ExtractionItemCategory Category => category;
        public Sprite Icon => icon;
        public int MaxStack => maxStack;

        private void OnValidate()
        {
            maxStack = Mathf.Max(1, maxStack);
        }
    }
}
