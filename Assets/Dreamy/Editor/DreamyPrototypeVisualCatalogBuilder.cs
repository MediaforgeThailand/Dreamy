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
        private const float WarriorSheetPixelsPerUnit = 128f;
        private const float UiPixelsPerUnit = 100f;

        static DreamyPrototypeVisualCatalogBuilder()
        {
            EditorApplication.delayCall += EnsureCatalog;
        }

        [MenuItem("Dreamy/Prototype/Refresh Runtime Visual Catalog")]
        public static void EnsureCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            EnsureFolder(ResourcesFolder);

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
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureUiSprite(string path)
        {
            ConfigureSprite(path, UiPixelsPerUnit);
        }

        private static void ConfigurePixelArtSheet(string path)
        {
            ConfigureSprite(path, WarriorSheetPixelsPerUnit);
        }

        private static void ConfigureSprite(string path, float pixelsPerUnit)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            bool changed = false;
            changed |= SetImporterValue(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            changed |= SetImporterValue(importer.spriteImportMode != SpriteImportMode.Single, () => importer.spriteImportMode = SpriteImportMode.Single);
            changed |= SetImporterValue(!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit), () => importer.spritePixelsPerUnit = pixelsPerUnit);
            changed |= SetImporterValue(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            changed |= SetImporterValue(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetImporterValue(importer.textureCompression != TextureImporterCompression.Uncompressed, () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(importer.npotScale != TextureImporterNPOTScale.None, () => importer.npotScale = TextureImporterNPOTScale.None);
            changed |= SetImporterValue(importer.alphaIsTransparency == false, () => importer.alphaIsTransparency = true);
            changed |= SetImporterValue(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetImporterValue(importer.anisoLevel != 0, () => importer.anisoLevel = 0);
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
