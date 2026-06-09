using Dreamy;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    [InitializeOnLoad]
    internal static class DreamyPrototypeVisualCatalogBuilder
    {
        private const string ResourcesFolder = "Assets/Resources";
        private const string CatalogPath = ResourcesFolder + "/DreamyPrototypeVisualCatalog.asset";
        private const string CraftingBackgroundPath = "Assets/Resources/BG_blacksmith.jpg";
        private static readonly string[] CraftingIconPaths =
        {
            "Assets/Resources/Dreamy/Crafting/Icons/Icon_GardenMeal.png",
            "Assets/Resources/Dreamy/Crafting/Icons/Icon_Crop.png",
            "Assets/Resources/Dreamy/Crafting/Icons/Icon_Wood.png"
        };
        private const float WarriorSheetPixelsPerUnit = 128f;
        private const float SampleCharacterPixelsPerUnit = 48f;
        private const float AxionCharacterPixelsPerUnit = 48f;
        private const float UiPixelsPerUnit = 100f;
        private const string AxionPremiumSpriteFolder = "Assets/Luneblade - Little Axion (Premium)/Sprite Sheet";
        private const string AxionAnimatorControllerPath = "Assets/Dreamy/Generated/AxionAnimator/LittleAxionPrototype.controller";

        static DreamyPrototypeVisualCatalogBuilder()
        {
            EditorApplication.delayCall += EnsureCatalog;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Dreamy/Prototype/Refresh Runtime Visual Catalog")]
        public static void EnsureCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureFolder(ResourcesFolder);

            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Idle.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Run.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack1.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack2.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack3.png");
            ConfigureSampleCharacterSheet("Assets/Dreamy/Characters/SampleIdleWalk/idle/sprite sheets/idle.png");
            ConfigureSampleCharacterSheet("Assets/Dreamy/Characters/SampleIdleWalk/walk/sprite sheets/walk.png");
            ConfigureAxionCharacterSheet(AxionSheetPath("Idle.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Run.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Dash.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Hurt.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Attack 1.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Attack 2.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Attack 3.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Super Smash.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Death.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Fall.png"));
            ConfigureAxionCharacterSheet(AxionSheetPath("Jump.png"));
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Idle.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Run.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Attack1.png");
            ConfigurePixelArtSheet("Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Attack2.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Papers/RegularPaper.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Wood Table/WoodTable_Slots.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Bars/BigBar_Base.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Bars/BigBar_Fill.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallRedRoundButton_Regular.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallRedRoundButton_Pressed.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Pressed.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_05.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_06.png");
            ConfigureUiSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_11.png");
            ConfigureBackgroundSprite(CraftingBackgroundPath);
            for (int i = 0; i < CraftingIconPaths.Length; i++)
            {
                ConfigureUiSprite(CraftingIconPaths[i]);
            }

            DreamyPrototypeVisualCatalog catalog = AssetDatabase.LoadAssetAtPath<DreamyPrototypeVisualCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DreamyPrototypeVisualCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            SetSprite(serialized, "woodSprite", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Stump 1.png");
            SetSprite(serialized, "goldSprite", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png");
            SetSprite(serialized, "foodSprite", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Meat/Meat Resource/Meat Resource.png");
            SetTexture(serialized, "playerIdleSheet", "Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Idle.png");
            SetTexture(serialized, "playerRunSheet", "Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Run.png");
            SetTexture(serialized, "playerAttackSheet", "Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack1.png");
            SetTexture(serialized, "playerAttack2Sheet", "Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack2.png");
            SetTexture(serialized, "playerAttack3Sheet", "Assets/Tiny Swords (Free Pack)/Units/Blue Units/Warrior/Warrior_Attack3.png");
            SetTexture(serialized, "sampleCharacterIdleSheet", "Assets/Dreamy/Characters/SampleIdleWalk/idle/sprite sheets/idle.png");
            SetTexture(serialized, "sampleCharacterWalkSheet", "Assets/Dreamy/Characters/SampleIdleWalk/walk/sprite sheets/walk.png");
            SetTexture(serialized, "axionCharacterIdleSheet", AxionSheetPath("Idle.png"));
            SetTexture(serialized, "axionCharacterRunSheet", AxionSheetPath("Run.png"));
            SetTexture(serialized, "axionCharacterDashSheet", AxionSheetPath("Dash.png"));
            SetTexture(serialized, "axionCharacterHurtSheet", AxionSheetPath("Hurt.png"));
            SetTexture(serialized, "axionCharacterAttackSheet", AxionSheetPath("Attack 1.png"));
            SetTexture(serialized, "axionCharacterAttack2Sheet", AxionSheetPath("Attack 2.png"));
            SetTexture(serialized, "axionCharacterAttack3Sheet", AxionSheetPath("Attack 3.png"));
            SetTexture(serialized, "axionCharacterSuperSmashSheet", AxionSheetPath("Super Smash.png"));
            SetObject<RuntimeAnimatorController>(serialized, "axionAnimatorController", AxionAnimatorControllerPath);
            SetSprite(serialized, "enemySprite", "Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Idle.png");
            SetTexture(serialized, "enemyIdleSheet", "Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Idle.png");
            SetTexture(serialized, "enemyRunSheet", "Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Run.png");
            SetTexture(serialized, "enemyAttackSheet", "Assets/Tiny Swords (Free Pack)/Units/Red Units/Warrior/Warrior_Attack1.png");
            SetSprite(serialized, "uiPanelSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Papers/RegularPaper.png");
            SetSprite(serialized, "uiSlotSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Wood Table/WoodTable_Slots.png");
            SetSprite(serialized, "uiBarBaseSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Bars/BigBar_Base.png");
            SetSprite(serialized, "uiBarFillSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Bars/BigBar_Fill.png");
            SetSprite(serialized, "uiRedButtonSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallRedRoundButton_Regular.png");
            SetSprite(serialized, "uiRedButtonPressedSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallRedRoundButton_Pressed.png");
            SetSprite(serialized, "uiBlueButtonSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
            SetSprite(serialized, "uiBlueButtonPressedSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Pressed.png");
            SetSprite(serialized, "uiAttackIconSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_05.png");
            SetSprite(serialized, "uiDodgeIconSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_06.png");
            SetSprite(serialized, "uiInventoryIconSprite", "Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Icons/Icon_11.png");
            SetSprite(serialized, "craftingBackgroundSprite", CraftingBackgroundPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.delayCall += EnsureCatalog;
            }
        }

        private static void ConfigureBackgroundSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            bool changed = false;
            changed |= SetImporterValue(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            changed |= SetImporterValue(importer.spriteImportMode != SpriteImportMode.Single, () => importer.spriteImportMode = SpriteImportMode.Single);
            changed |= SetImporterValue(!Mathf.Approximately(importer.spritePixelsPerUnit, UiPixelsPerUnit), () => importer.spritePixelsPerUnit = UiPixelsPerUnit);
            changed |= SetImporterValue(importer.filterMode != FilterMode.Bilinear, () => importer.filterMode = FilterMode.Bilinear);
            changed |= SetImporterValue(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetImporterValue(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetImporterValue(importer.npotScale != TextureImporterNPOTScale.None, () => importer.npotScale = TextureImporterNPOTScale.None);
            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void ConfigureUiSprite(string path)
        {
            ConfigureSprite(path, UiPixelsPerUnit, true);
        }

        private static void ConfigurePixelArtSheet(string path)
        {
            ConfigureSprite(path, WarriorSheetPixelsPerUnit, false);
        }

        private static void ConfigureSampleCharacterSheet(string path)
        {
            ConfigureSprite(path, SampleCharacterPixelsPerUnit, false);
        }

        private static void ConfigureAxionCharacterSheet(string path)
        {
            ConfigureSprite(path, AxionCharacterPixelsPerUnit, false, false);
        }

        private static string AxionSheetPath(string fileName)
        {
            return AxionPremiumSpriteFolder + "/" + fileName;
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit, bool readable)
        {
            ConfigureSprite(path, pixelsPerUnit, readable, true);
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit, bool readable, bool forceSingleSpriteMode)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            bool changed = false;
            changed |= SetImporterValue(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            if (forceSingleSpriteMode)
            {
                changed |= SetImporterValue(importer.spriteImportMode != SpriteImportMode.Single, () => importer.spriteImportMode = SpriteImportMode.Single);
            }

            changed |= SetImporterValue(!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit), () => importer.spritePixelsPerUnit = pixelsPerUnit);
            changed |= SetImporterValue(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            changed |= SetImporterValue(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetImporterValue(importer.textureCompression != TextureImporterCompression.Uncompressed, () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(importer.npotScale != TextureImporterNPOTScale.None, () => importer.npotScale = TextureImporterNPOTScale.None);
            changed |= SetImporterValue(importer.alphaIsTransparency == false, () => importer.alphaIsTransparency = true);
            changed |= SetImporterValue(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetImporterValue(importer.anisoLevel != 0, () => importer.anisoLevel = 0);
            changed |= SetImporterValue(importer.isReadable != readable, () => importer.isReadable = readable);
            if (pixelsPerUnit <= WarriorSheetPixelsPerUnit)
            {
                changed |= ConfigureUncompressedPlatform(importer, "Standalone");
                changed |= ConfigureUncompressedPlatform(importer, "Android");
                changed |= ConfigureUncompressedPlatform(importer, "iPhone");
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static bool ConfigureUncompressedPlatform(TextureImporter importer, string platformName)
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
            bool changed = false;
            changed |= SetImporterValue(!settings.overridden, () => settings.overridden = true);
            changed |= SetImporterValue(settings.maxTextureSize < 4096, () => settings.maxTextureSize = 4096);
            changed |= SetImporterValue(settings.format != TextureImporterFormat.RGBA32, () => settings.format = TextureImporterFormat.RGBA32);
            changed |= SetImporterValue(settings.textureCompression != TextureImporterCompression.Uncompressed, () => settings.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(settings.compressionQuality != 100, () => settings.compressionQuality = 100);

            if (changed)
            {
                importer.SetPlatformTextureSettings(settings);
            }

            return changed;
        }

        private static bool SetImporterValue(bool shouldSet, System.Action apply)
        {
            if (!shouldSet)
            {
                return false;
            }

            apply();
            return true;
        }

        private static void SetSprite(SerializedObject serialized, string propertyName, string spritePath)
        {
            Sprite sprite = LoadSprite(spritePath);
            if (sprite == null)
            {
                return;
            }

            serialized.FindProperty(propertyName).objectReferenceValue = sprite;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite loadedSprite)
                {
                    return loadedSprite;
                }
            }

            return null;
        }

        private static void SetTexture(SerializedObject serialized, string propertyName, string texturePath)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                serialized.FindProperty(propertyName).objectReferenceValue = texture;
            }
        }

        private static void SetObject<T>(SerializedObject serialized, string propertyName, string assetPath) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                serialized.FindProperty(propertyName).objectReferenceValue = asset;
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int slash = folderPath.LastIndexOf('/');
            string parent = slash > 0 ? folderPath.Substring(0, slash) : "Assets";
            string folderName = folderPath.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
