using UnityEngine;

namespace Dreamy
{
    [CreateAssetMenu(menuName = "Dreamy/Prototype/Visual Catalog", fileName = "DreamyPrototypeVisualCatalog")]
    public sealed class DreamyPrototypeVisualCatalog : ScriptableObject
    {
        [SerializeField] private Sprite woodSprite;
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private Sprite foodSprite;
        [SerializeField] private Sprite enemySprite;
        [SerializeField] private Texture2D enemyIdleSheet;
        [SerializeField] private Texture2D enemyRunSheet;
        [SerializeField] private Texture2D enemyAttackSheet;
        [SerializeField] private Sprite uiPanelSprite;
        [SerializeField] private Sprite uiSlotSprite;
        [SerializeField] private Sprite uiBarBaseSprite;
        [SerializeField] private Sprite uiBarFillSprite;
        [SerializeField] private Sprite uiRedButtonSprite;
        [SerializeField] private Sprite uiRedButtonPressedSprite;
        [SerializeField] private Sprite uiBlueButtonSprite;
        [SerializeField] private Sprite uiBlueButtonPressedSprite;
        [SerializeField] private Sprite uiAttackIconSprite;
        [SerializeField] private Sprite uiDodgeIconSprite;
        [SerializeField] private Sprite uiInventoryIconSprite;

        public Sprite WoodSprite => woodSprite;
        public Sprite GoldSprite => goldSprite;
        public Sprite FoodSprite => foodSprite;
        public Sprite EnemySprite => enemySprite;
        public Texture2D EnemyIdleSheet => enemyIdleSheet;
        public Texture2D EnemyRunSheet => enemyRunSheet;
        public Texture2D EnemyAttackSheet => enemyAttackSheet;
        public Sprite UiPanelSprite => uiPanelSprite;
        public Sprite UiSlotSprite => uiSlotSprite;
        public Sprite UiBarBaseSprite => uiBarBaseSprite;
        public Sprite UiBarFillSprite => uiBarFillSprite;
        public Sprite UiRedButtonSprite => uiRedButtonSprite;
        public Sprite UiRedButtonPressedSprite => uiRedButtonPressedSprite;
        public Sprite UiBlueButtonSprite => uiBlueButtonSprite;
        public Sprite UiBlueButtonPressedSprite => uiBlueButtonPressedSprite;
        public Sprite UiAttackIconSprite => uiAttackIconSprite;
        public Sprite UiDodgeIconSprite => uiDodgeIconSprite;
        public Sprite UiInventoryIconSprite => uiInventoryIconSprite;

        public Sprite GetItemSprite(DreamyItemId itemId)
        {
            switch (itemId)
            {
                case DreamyItemId.Wood:
                    return woodSprite;
                case DreamyItemId.Gold:
                    return goldSprite;
                case DreamyItemId.Food:
                case DreamyItemId.Meat:
                case DreamyItemId.Seed:
                case DreamyItemId.Crop:
                case DreamyItemId.CraftedMeal:
                    return foodSprite;
                case DreamyItemId.CraftedTool:
                    return woodSprite != null ? woodSprite : goldSprite;
                default:
                    return woodSprite != null ? woodSprite : goldSprite != null ? goldSprite : foodSprite;
            }
        }
    }
}
