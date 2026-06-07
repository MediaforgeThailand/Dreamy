using System;
using System.Collections.Generic;
using System.IO;
using Dreamy;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    [InitializeOnLoad]
    internal static class DreamyMonsterCatalogBuilder
    {
        private const string EnemyRoot = "Assets/Tiny Swords (Enemy Pack)/Enemy Pack/Enemies";
        private const string ResourcesFolder = "Assets/Resources";
        private const string GeneratedFolder = "Assets/Dreamy/Generated/MonsterDefinitions";
        private const string CatalogPath = ResourcesFolder + "/DreamyMonsterCatalog.asset";
        private const float DefaultPixelsPerUnit = 192f;
        private const float DefaultVisualScale = 1.45f;

        static DreamyMonsterCatalogBuilder()
        {
            EditorApplication.delayCall += EnsureCatalog;
        }

        [MenuItem("Dreamy/Prototype/Refresh Monster Catalog")]
        public static void EnsureCatalog()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !AssetDatabase.IsValidFolder(EnemyRoot))
            {
                return;
            }

            EnsureFolder(ResourcesFolder);
            EnsureFolder(GeneratedFolder);

            string[] files = Directory.GetFiles(EnemyRoot, "*.png", SearchOption.AllDirectories);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            Dictionary<string, List<string>> filesByFolder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = NormalizeAssetPath(files[i]);
                ConfigurePixelArtSheet(assetPath);

                string folder = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? EnemyRoot;
                if (!filesByFolder.TryGetValue(folder, out List<string> group))
                {
                    group = new List<string>();
                    filesByFolder.Add(folder, group);
                }

                group.Add(assetPath);
            }

            List<DreamyMonsterDefinition> definitions = new List<DreamyMonsterDefinition>();
            foreach (KeyValuePair<string, List<string>> pair in filesByFolder)
            {
                DreamyMonsterDefinition definition = CreateOrUpdateDefinition(pair.Key, pair.Value);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            definitions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            DreamyMonsterCatalog catalog = AssetDatabase.LoadAssetAtPath<DreamyMonsterCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<DreamyMonsterCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty monsters = serializedCatalog.FindProperty("monsters");
            monsters.arraySize = definitions.Count;
            for (int i = 0; i < definitions.Count; i++)
            {
                monsters.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            }

            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
        }

        private static DreamyMonsterDefinition CreateOrUpdateDefinition(string folderPath, List<string> files)
        {
            string idlePath = FindFirst(files, "_Idle");
            if (string.IsNullOrEmpty(idlePath))
            {
                return null;
            }

            string displayName = Path.GetFileName(folderPath);
            string relativeFolder = folderPath.StartsWith(EnemyRoot, StringComparison.OrdinalIgnoreCase)
                ? folderPath.Substring(EnemyRoot.Length).Trim('/')
                : displayName;
            string monsterId = ToId(relativeFolder);
            string assetPath = GeneratedFolder + "/" + monsterId + ".asset";

            DreamyMonsterDefinition definition = AssetDatabase.LoadAssetAtPath<DreamyMonsterDefinition>(assetPath);
            bool created = definition == null;
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<DreamyMonsterDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            Texture2D idle = LoadTexture(idlePath);
            Texture2D run = LoadTexture(FindFirst(files, "_Run"));
            Texture2D walk = LoadTexture(FindFirst(files, "_Walk"));
            Texture2D attack = LoadTexture(FindAttack(files));
            Texture2D hit = LoadTexture(FindFirst(files, "_Hit"));
            Texture2D death = LoadTexture(FindFirst(files, "_Dead", "_Death"));

            SerializedObject serialized = new SerializedObject(definition);
            SetString(serialized, "monsterId", monsterId);
            SetString(serialized, "displayName", displayName);
            SetTexture(serialized, "idleSheet", idle);
            SetTexture(serialized, "runSheet", run);
            SetTexture(serialized, "walkSheet", walk);
            SetTexture(serialized, "attackSheet", attack);
            SetTexture(serialized, "hitSheet", hit);
            SetTexture(serialized, "deathSheet", death);
            SetInt(serialized, "idleFrameCount", EstimateFrameCount(idle));
            SetInt(serialized, "runFrameCount", EstimateFrameCount(run));
            SetInt(serialized, "walkFrameCount", EstimateFrameCount(walk));
            SetInt(serialized, "attackFrameCount", EstimateFrameCount(attack));
            SetInt(serialized, "hitFrameCount", EstimateFrameCount(hit));
            SetInt(serialized, "deathFrameCount", EstimateFrameCount(death));
            SetDefaultFloat(serialized, "visualScale", DefaultVisualScale);

            if (created)
            {
                float frameHeight = idle != null ? idle.height : 192f;
                SetInt(serialized, "level", Mathf.Clamp(Mathf.RoundToInt(frameHeight / 16f), 1, 50));
                SetFloat(serialized, "maxHealth", Mathf.Round(34f + frameHeight * 0.12f));
                SetFloat(serialized, "damage", Mathf.Round(5f + frameHeight * 0.02f));
                SetFloat(serialized, "chaseSpeed", Mathf.Clamp(2.45f - frameHeight * 0.0014f, 1.55f, 2.25f));
                SetFloat(serialized, "detectionRange", 7.5f);
                SetFloat(serialized, "attackRange", Mathf.Clamp(0.72f + frameHeight * 0.0011f, 0.85f, 1.18f));
                SetFloat(serialized, "knockbackResistance", Mathf.Clamp(frameHeight / 96f, 1.25f, 4.5f));
                SetFloat(serialized, "pixelsPerUnit", DefaultPixelsPerUnit);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static string FindAttack(List<string> files)
        {
            string attack = FindFirst(files, "_Attack Fast");
            if (!string.IsNullOrEmpty(attack))
            {
                return attack;
            }

            attack = FindFirst(files, "_Attack");
            if (!string.IsNullOrEmpty(attack))
            {
                return attack;
            }

            attack = FindFirst(files, "_Throw");
            if (!string.IsNullOrEmpty(attack))
            {
                return attack;
            }

            return FindFirst(files, "_Shoot");
        }

        private static string FindFirst(List<string> files, params string[] tokens)
        {
            for (int i = 0; i < files.Count; i++)
            {
                string name = Path.GetFileNameWithoutExtension(files[i]);
                for (int tokenIndex = 0; tokenIndex < tokens.Length; tokenIndex++)
                {
                    if (name.IndexOf(tokens[tokenIndex], StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return files[i];
                    }
                }
            }

            return null;
        }

        private static Texture2D LoadTexture(string assetPath)
        {
            return string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static int EstimateFrameCount(Texture2D texture)
        {
            if (texture == null || texture.height <= 0)
            {
                return 1;
            }

            int count = texture.width / texture.height;
            return Mathf.Max(1, count);
        }

        private static void ConfigurePixelArtSheet(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            bool changed = false;
            changed |= SetImporterValue(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            changed |= SetImporterValue(importer.spriteImportMode != SpriteImportMode.Single, () => importer.spriteImportMode = SpriteImportMode.Single);
            changed |= SetImporterValue(!Mathf.Approximately(importer.spritePixelsPerUnit, DefaultPixelsPerUnit), () => importer.spritePixelsPerUnit = DefaultPixelsPerUnit);
            changed |= SetImporterValue(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            changed |= SetImporterValue(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetImporterValue(importer.textureCompression != TextureImporterCompression.Uncompressed, () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(importer.npotScale != TextureImporterNPOTScale.None, () => importer.npotScale = TextureImporterNPOTScale.None);
            changed |= SetImporterValue(importer.alphaIsTransparency == false, () => importer.alphaIsTransparency = true);
            changed |= SetImporterValue(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetImporterValue(importer.anisoLevel != 0, () => importer.anisoLevel = 0);
            changed |= SetImporterValue(importer.maxTextureSize < 8192, () => importer.maxTextureSize = 8192);
            changed |= ConfigureUncompressedPlatform(importer, "Standalone");
            changed |= ConfigureUncompressedPlatform(importer, "Android");
            changed |= ConfigureUncompressedPlatform(importer, "iPhone");

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
            changed |= SetImporterValue(settings.maxTextureSize < 8192, () => settings.maxTextureSize = 8192);
            changed |= SetImporterValue(settings.format != TextureImporterFormat.RGBA32, () => settings.format = TextureImporterFormat.RGBA32);
            changed |= SetImporterValue(settings.textureCompression != TextureImporterCompression.Uncompressed, () => settings.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(settings.compressionQuality != 100, () => settings.compressionQuality = 100);

            if (changed)
            {
                importer.SetPlatformTextureSettings(settings);
            }

            return changed;
        }

        private static bool SetImporterValue(bool shouldSet, Action apply)
        {
            if (!shouldSet)
            {
                return false;
            }

            apply();
            return true;
        }

        private static string NormalizeAssetPath(string path)
        {
            string fullPath = Path.GetFullPath(path).Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            return fullPath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase)
                ? "Assets" + fullPath.Substring(dataPath.Length)
                : path.Replace('\\', '/');
        }

        private static string ToId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "enemy";
            }

            char[] chars = value.ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]))
                {
                    chars[i] = '_';
                }
            }

            string id = new string(chars).Trim('_');
            while (id.Contains("__"))
            {
                id = id.Replace("__", "_");
            }

            return string.IsNullOrEmpty(id) ? "enemy" : id;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            serialized.FindProperty(propertyName).stringValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            serialized.FindProperty(propertyName).intValue = Mathf.Max(1, value);
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            serialized.FindProperty(propertyName).floatValue = value;
        }

        private static void SetDefaultFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && property.floatValue <= 0f)
            {
                property.floatValue = value;
            }
        }

        private static void SetTexture(SerializedObject serialized, string propertyName, Texture2D texture)
        {
            serialized.FindProperty(propertyName).objectReferenceValue = texture;
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
