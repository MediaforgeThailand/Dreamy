using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dreamy
{
    [Serializable]
    public sealed class DreamyPrototypeRecipe
    {
        [SerializeField] private string recipeId;
        [SerializeField] private string displayName;
        [SerializeField] private List<DreamyItemStack> inputs = new List<DreamyItemStack>();
        [SerializeField] private List<DreamyItemStack> outputs = new List<DreamyItemStack>();

        public string RecipeId => string.IsNullOrWhiteSpace(recipeId) ? DisplayName : recipeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? "Recipe" : displayName;
        public IReadOnlyList<DreamyItemStack> Inputs => inputs;
        public IReadOnlyList<DreamyItemStack> Outputs => outputs;

        public DreamyPrototypeRecipe()
        {
        }

        public DreamyPrototypeRecipe(string recipeId, string displayName, IEnumerable<DreamyItemStack> inputs, IEnumerable<DreamyItemStack> outputs)
        {
            this.recipeId = recipeId;
            this.displayName = displayName;
            this.inputs = new List<DreamyItemStack>(inputs);
            this.outputs = new List<DreamyItemStack>(outputs);
            Validate();
        }

        public void Validate()
        {
            inputs.RemoveAll(item => item == null || !item.IsValid);
            outputs.RemoveAll(item => item == null || !item.IsValid);
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].Validate();
            }

            for (int i = 0; i < outputs.Count; i++)
            {
                outputs[i].Validate();
            }
        }
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DreamyPrototypeCraftingStation : DreamyPrototypeInteractable
    {
        private const int RuntimeSortingOrder = 100;
        private const float RuntimeSortingUnitsPerWorldUnit = 10f;

        [SerializeField] private string stationDisplayName = "Crafting Keeper";
        [SerializeField] private Texture2D npcIdleSheet;
        [SerializeField] private int idleFrameCount = 8;
        [SerializeField] private float pixelsPerUnit = 128f;
        [SerializeField] private List<DreamyPrototypeRecipe> recipes = new List<DreamyPrototypeRecipe>();

        private SpriteRenderer spriteRenderer;
        private TextMesh label;

        public IReadOnlyList<DreamyPrototypeRecipe> Recipes => recipes;
        public string DisplayName => string.IsNullOrWhiteSpace(stationDisplayName) ? "Crafting Keeper" : stationDisplayName;
        public override string InteractionLabel => "Talk to " + DisplayName;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (stationDisplayName == "Crafting Table")
            {
                stationDisplayName = "Crafting Keeper";
            }

            EnsureYSort();
            EnsureLabel();
            RefreshVisual();
            EnsureDefaultRecipes();
        }

        public void Configure(DreamyPrototypeVisualCatalog catalog)
        {
            npcIdleSheet = catalog != null ? catalog.EnemyIdleSheet : npcIdleSheet;
            RefreshVisual();
        }

        public override bool Interact(DreamyMobilePlayer player)
        {
            if (player == null)
            {
                return false;
            }

            DreamyPrototypeInteractionUi.OpenCrafting(this, player);
            return true;
        }

        public bool TryCraft(DreamyPrototypeRecipe recipe, DreamyInventory inventory, out string message)
        {
            message = string.Empty;
            if (recipe == null || inventory == null)
            {
                message = "Missing recipe";
                return false;
            }

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                DreamyItemStack input = recipe.Inputs[i];
                if (inventory.GetQuantity(input.ItemId) < input.Quantity)
                {
                    message = "Need " + input.DisplayName + " x" + input.Quantity;
                    return false;
                }
            }

            if (!CanInventoryFitRecipeOutputs(recipe, inventory))
            {
                message = "Inventory full";
                return false;
            }

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                DreamyItemStack input = recipe.Inputs[i];
                inventory.RemoveItem(input.ItemId, input.Quantity);
            }

            for (int i = 0; i < recipe.Outputs.Count; i++)
            {
                DreamyItemStack output = recipe.Outputs[i];
                if (!inventory.AddItem(output.ItemId, output.Quantity, output.DisplayName))
                {
                    message = "Inventory full";
                    return false;
                }
            }

            message = "Crafted " + recipe.DisplayName;
            return true;
        }

        private static bool CanInventoryFitRecipeOutputs(DreamyPrototypeRecipe recipe, DreamyInventory inventory)
        {
            if (recipe == null || inventory == null)
            {
                return false;
            }

            List<DreamyItemStack> virtualSlots = new List<DreamyItemStack>();
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                DreamyInventorySlot slot = inventory.Items[i];
                if (slot != null && slot.Quantity > 0)
                {
                    virtualSlots.Add(new DreamyItemStack(slot.ItemId, slot.DisplayName, slot.Quantity));
                }
            }

            for (int i = 0; i < recipe.Inputs.Count; i++)
            {
                DreamyItemStack input = recipe.Inputs[i];
                SubtractFromVirtualSlots(virtualSlots, input.ItemId, input.Quantity);
            }

            for (int i = 0; i < recipe.Outputs.Count; i++)
            {
                DreamyItemStack output = recipe.Outputs[i];
                int existingIndex = FindVirtualSlotIndex(virtualSlots, output.ItemId);
                if (existingIndex >= 0)
                {
                    continue;
                }

                if (virtualSlots.Count >= inventory.MaxSlots)
                {
                    return false;
                }

                virtualSlots.Add(new DreamyItemStack(output.ItemId, output.DisplayName, output.Quantity));
            }

            return true;
        }

        private static void SubtractFromVirtualSlots(List<DreamyItemStack> slots, DreamyItemId itemId, int quantity)
        {
            int index = FindVirtualSlotIndex(slots, itemId);
            if (index < 0)
            {
                return;
            }

            int remainingQuantity = slots[index].Quantity - Mathf.Max(0, quantity);
            slots.RemoveAt(index);
            if (remainingQuantity > 0)
            {
                slots.Add(new DreamyItemStack(itemId, itemId.ToString(), remainingQuantity));
            }
        }

        private static int FindVirtualSlotIndex(List<DreamyItemStack> slots, DreamyItemId itemId)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null && slots[i].ItemId == itemId)
                {
                    return i;
                }
            }

            return -1;
        }

        public string GetRecipeSummary(DreamyPrototypeRecipe recipe)
        {
            if (recipe == null)
            {
                return string.Empty;
            }

            return FormatStacks(recipe.Inputs) + " -> " + FormatStacks(recipe.Outputs);
        }

        private static string FormatStacks(IReadOnlyList<DreamyItemStack> stacks)
        {
            if (stacks == null || stacks.Count == 0)
            {
                return "-";
            }

            string value = string.Empty;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (i > 0)
                {
                    value += " + ";
                }

                value += stacks[i].DisplayName + " x" + stacks[i].Quantity;
            }

            return value;
        }

        private void EnsureDefaultRecipes()
        {
            if (recipes.Count > 0)
            {
                return;
            }

            recipes.Add(new DreamyPrototypeRecipe(
                "seed_pack",
                "Seed Pack",
                new[] { new DreamyItemStack(DreamyItemId.Food, "Food", 1) },
                new[] { new DreamyItemStack(DreamyItemId.Seed, "Seed", 2) }));
            recipes.Add(new DreamyPrototypeRecipe(
                "garden_meal",
                "Garden Meal",
                new[]
                {
                    new DreamyItemStack(DreamyItemId.Crop, "Crop", 2),
                    new DreamyItemStack(DreamyItemId.Wood, "Wood", 1)
                },
                new[] { new DreamyItemStack(DreamyItemId.CraftedMeal, "Garden Meal", 1) }));
            recipes.Add(new DreamyPrototypeRecipe(
                "field_tool",
                "Field Tool",
                new[]
                {
                    new DreamyItemStack(DreamyItemId.Wood, "Wood", 2),
                    new DreamyItemStack(DreamyItemId.Gold, "Gold", 1)
                },
                new[] { new DreamyItemStack(DreamyItemId.CraftedTool, "Field Tool", 1) }));
        }

        private void RefreshVisual()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = npcIdleSheet != null ? CreateFirstFrame(npcIdleSheet, idleFrameCount, pixelsPerUnit) : CreateFallbackCraftingNpcSprite();
            spriteRenderer.color = Color.white;
            spriteRenderer.drawMode = SpriteDrawMode.Simple;
            EnsureYSort();
        }

        private void EnsureYSort()
        {
            DreamyYSortSprite ySort = GetComponent<DreamyYSortSprite>();
            if (ySort == null)
            {
                ySort = gameObject.AddComponent<DreamyYSortSprite>();
            }

            ySort.Configure(RuntimeSortingOrder, RuntimeSortingUnitsPerWorldUnit);
        }

        private void EnsureLabel()
        {
            if (label != null)
            {
                return;
            }

            Transform existing = transform.Find("Crafting Label");
            GameObject labelObject = existing != null ? existing.gameObject : new GameObject("Crafting Label");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.82f, 0f);
            label = labelObject.GetComponent<TextMesh>();
            if (label == null)
            {
                label = labelObject.AddComponent<TextMesh>();
            }

            label.text = DisplayName;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.12f;
            label.fontSize = 28;
            label.color = Color.white;
            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 245;
            }
        }

        private static Sprite CreateFirstFrame(Texture2D texture, int frameCount, float pixelsPerUnit)
        {
            if (texture == null)
            {
                return CreateFallbackCraftingNpcSprite();
            }

            texture.filterMode = FilterMode.Point;
            int frames = Mathf.Max(1, frameCount);
            int width = Mathf.Max(1, texture.width / frames);
            Rect rect = new Rect(0f, 0f, width, texture.height);
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.28f), Mathf.Max(1f, pixelsPerUnit), 0, SpriteMeshType.FullRect);
        }

        private static Sprite CreateFallbackCraftingNpcSprite()
        {
            Texture2D texture = new Texture2D(14, 18);
            Color apron = new Color(0.62f, 0.36f, 0.16f, 1f);
            Color shirt = new Color(0.78f, 0.72f, 0.58f, 1f);
            Color face = new Color(0.95f, 0.72f, 0.48f, 1f);
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color color = apron;
                    if (y > 11 && x > 3 && x < 10)
                    {
                        color = face;
                    }
                    else if (y > 7 && x > 2 && x < 11)
                    {
                        color = shirt;
                    }

                    texture.SetPixel(x, y, color);
                }
            }

            texture.filterMode = FilterMode.Point;
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.2f), 18f);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            idleFrameCount = Mathf.Max(1, idleFrameCount);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            recipes.RemoveAll(recipe => recipe == null);
            for (int i = 0; i < recipes.Count; i++)
            {
                recipes[i].Validate();
            }
        }
    }
}
