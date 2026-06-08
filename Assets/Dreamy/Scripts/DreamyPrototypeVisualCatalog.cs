using UnityEngine;

namespace Dreamy
{
    [CreateAssetMenu(menuName = "Dreamy/Prototype/Visual Catalog", fileName = "DreamyPrototypeVisualCatalog")]
    public sealed class DreamyPrototypeVisualCatalog : ScriptableObject
    {
        [SerializeField] private Sprite woodSprite;
        [SerializeField] private Sprite goldSprite;
        [SerializeField] private Sprite foodSprite;
        [SerializeField] private Texture2D playerIdleSheet;
        [SerializeField] private Texture2D playerRunSheet;
        [SerializeField] private Texture2D playerAttackSheet;
        [SerializeField] private Texture2D playerAttack2Sheet;
        [SerializeField] private Texture2D playerAttack3Sheet;
        [SerializeField] private Texture2D sampleCharacterIdleSheet;
        [SerializeField] private Texture2D sampleCharacterWalkSheet;
        [SerializeField] private Texture2D axionCharacterIdleSheet;
        [SerializeField] private Texture2D axionCharacterRunSheet;
        [SerializeField] private Texture2D axionCharacterAttackSheet;
        [SerializeField] private Texture2D axionCharacterAttack2Sheet;
        [SerializeField] private Texture2D axionCharacterAttack3Sheet;
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
        [SerializeField] private Sprite craftingBackgroundSprite;

        public Sprite WoodSprite => woodSprite;
        public Sprite GoldSprite => goldSprite;
        public Sprite FoodSprite => foodSprite;
        public Texture2D PlayerIdleSheet => playerIdleSheet != null ? playerIdleSheet : enemyIdleSheet;
        public Texture2D PlayerRunSheet => playerRunSheet != null ? playerRunSheet : enemyRunSheet;
        public Texture2D PlayerAttackSheet => playerAttackSheet != null ? playerAttackSheet : enemyAttackSheet;
        public Texture2D[] PlayerAttackSheets => BuildSheetList(PlayerAttackSheet, playerAttack2Sheet, playerAttack3Sheet);
        public Texture2D SampleCharacterIdleSheet => sampleCharacterIdleSheet;
        public Texture2D SampleCharacterWalkSheet => sampleCharacterWalkSheet;
        public bool HasSampleCharacter => sampleCharacterIdleSheet != null && sampleCharacterWalkSheet != null;
        public Texture2D AxionCharacterIdleSheet => axionCharacterIdleSheet;
        public Texture2D AxionCharacterRunSheet => axionCharacterRunSheet;
        public Texture2D AxionCharacterAttackSheet => axionCharacterAttackSheet;
        public Texture2D[] AxionCharacterAttackSheets => BuildSheetList(axionCharacterAttackSheet, axionCharacterAttack2Sheet, axionCharacterAttack3Sheet);
        public bool HasAxionCharacter => axionCharacterIdleSheet != null && axionCharacterRunSheet != null;
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
        public Sprite CraftingBackgroundSprite => craftingBackgroundSprite;

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
                case DreamyItemId.Coin:
                case DreamyItemId.UnlockToken:
                case DreamyItemId.SkillBook:
                    return goldSprite != null ? goldSprite : woodSprite;
                default:
                    return woodSprite != null ? woodSprite : goldSprite != null ? goldSprite : foodSprite;
            }
        }

        private static Texture2D[] BuildSheetList(Texture2D first, Texture2D second, Texture2D third)
        {
            int count = 0;
            if (first != null)
            {
                count++;
            }

            if (second != null)
            {
                count++;
            }

            if (third != null)
            {
                count++;
            }

            if (count == 0)
            {
                return System.Array.Empty<Texture2D>();
            }

            Texture2D[] result = new Texture2D[count];
            int index = 0;
            if (first != null)
            {
                result[index++] = first;
            }

            if (second != null)
            {
                result[index++] = second;
            }

            if (third != null)
            {
                result[index] = third;
            }

            return result;
        }
    }
}
