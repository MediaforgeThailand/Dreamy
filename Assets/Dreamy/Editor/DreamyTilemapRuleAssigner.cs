using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dreamy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Dreamy.Editor
{
    public static class DreamyTilemapRuleAssigner
    {
        private const string SpritefusionMapPath = "Assets/Spritefusion/Maps/map.prefab";
        private const string SpritefusionTilesFolder = "Assets/Spritefusion/Resources/tiles";
        private const string AutoApplySessionKey = "Dreamy.TinySwordsTilemapRulesApplied";

        private static readonly string[] OfficialTilemapTextures =
        {
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Water Background color.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Water Foam.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Shadow.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color1.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color2.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color3.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color4.png",
            "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color5.png"
        };

        [InitializeOnLoadMethod]
        private static void ApplyOnceAfterReload()
        {
            if (SessionState.GetBool(AutoApplySessionKey, false))
            {
                return;
            }

            SessionState.SetBool(AutoApplySessionKey, true);
            EditorApplication.delayCall += ApplyTinySwordsTilemapRules;
        }

        [MenuItem("Dreamy/Apply Tiny Swords Tilemap Rules")]
        public static void ApplyTinySwordsTilemapRules()
        {
            ApplyOfficialTextureImportRules();
            int taggedTileCount = ApplySpritefusionCustomTileAttributes();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Applied Tiny Swords tilemap rules to " + taggedTileCount + " Spritefusion tile assets.");
        }

        private static void ApplyOfficialTextureImportRules()
        {
            for (int i = 0; i < OfficialTilemapTextures.Length; i++)
            {
                TextureImporter importer = AssetImporter.GetAtPath(OfficialTilemapTextures[i]) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }

                if (!Mathf.Approximately(importer.spritePixelsPerUnit, DreamyLevelTileRules.PixelsPerUnit))
                {
                    importer.spritePixelsPerUnit = DreamyLevelTileRules.PixelsPerUnit;
                    changed = true;
                }

                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    changed = true;
                }

                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }

                if (changed)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static int ApplySpritefusionCustomTileAttributes()
        {
            Dictionary<CustomTile, List<DreamyTileLayerPolicy>> tileUsages = CollectSpritefusionTileUsage();
            List<CustomTile> allTiles = LoadAllSpritefusionCustomTiles();

            for (int i = 0; i < allTiles.Count; i++)
            {
                CustomTile tile = allTiles[i];
                tileUsages.TryGetValue(tile, out List<DreamyTileLayerPolicy> policies);
                ApplyAttributes(tile, policies);
                EditorUtility.SetDirty(tile);
            }

            return allTiles.Count;
        }

        private static Dictionary<CustomTile, List<DreamyTileLayerPolicy>> CollectSpritefusionTileUsage()
        {
            Dictionary<CustomTile, List<DreamyTileLayerPolicy>> usage = new Dictionary<CustomTile, List<DreamyTileLayerPolicy>>();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SpritefusionMapPath);
            if (prefab == null)
            {
                return usage;
            }

            Tilemap[] tilemaps = prefab.GetComponentsInChildren<Tilemap>(true);
            for (int i = 0; i < tilemaps.Length; i++)
            {
                Tilemap tilemap = tilemaps[i];
                DreamyTileLayerPolicy policy = DreamyLevelTileRules.GetLayerPolicy(tilemap.gameObject.name);
                BoundsInt bounds = tilemap.cellBounds;

                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    for (int y = bounds.yMin; y < bounds.yMax; y++)
                    {
                        Vector3Int position = new Vector3Int(x, y, 0);
                        CustomTile tile = tilemap.GetTile(position) as CustomTile;
                        if (tile == null)
                        {
                            continue;
                        }

                        if (!usage.TryGetValue(tile, out List<DreamyTileLayerPolicy> policies))
                        {
                            policies = new List<DreamyTileLayerPolicy>();
                            usage.Add(tile, policies);
                        }

                        if (!policies.Any(existing => existing.LayerName == policy.LayerName))
                        {
                            policies.Add(policy);
                        }
                    }
                }
            }

            return usage;
        }

        private static List<CustomTile> LoadAllSpritefusionCustomTiles()
        {
            List<CustomTile> tiles = new List<CustomTile>();
            if (!Directory.Exists(SpritefusionTilesFolder))
            {
                return tiles;
            }

            string[] assetPaths = Directory.GetFiles(SpritefusionTilesFolder, "tile_*.asset", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < assetPaths.Length; i++)
            {
                string assetPath = assetPaths[i].Replace("\\", "/");
                CustomTile tile = AssetDatabase.LoadAssetAtPath<CustomTile>(assetPath);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        private static void ApplyAttributes(CustomTile tile, List<DreamyTileLayerPolicy> policies)
        {
            bool hasPolicy = policies != null && policies.Count > 0;
            bool requiresLayerContext = !hasPolicy || policies.Count > 1 || policies.Any(policy => policy.CollisionRole == DreamyTileCollisionRole.Contextual);
            bool anyBlocksMovement = hasPolicy && policies.Any(policy => policy.BlocksMovement);
            bool allWalkable = hasPolicy && policies.All(policy => policy.Walkable);
            int elevationLevel = hasPolicy ? policies.Max(policy => policy.ElevationLevel) : 0;
            int renderBand = hasPolicy ? policies.Max(policy => policy.SortingBand) : 50;
            string guideRole = hasPolicy ? string.Join("|", policies.Select(policy => policy.GuideRole.ToString()).Distinct()) : DreamyTileGuideRole.Unknown.ToString();
            string colliderPolicy = requiresLayerContext && !anyBlocksMovement ? "contextual" : (anyBlocksMovement ? "blockMovement" : "none");
            string layerNames = hasPolicy ? string.Join("|", policies.Select(policy => policy.LayerName).Distinct()) : "";

            tile.attributes = new List<CustomTile.Attribute>
            {
                Attribute("assetId", tile.name),
                Attribute("guideSource", DreamyLevelTileRules.GuideSource),
                Attribute("tileSizePixels", DreamyLevelTileRules.TileSizePixels.ToString()),
                Attribute("pixelsPerUnit", DreamyLevelTileRules.PixelsPerUnit.ToString("0")),
                Attribute("guideRole", guideRole),
                Attribute("tilemapLayers", layerNames),
                Attribute("walkable", hasPolicy && allWalkable ? "true" : (hasPolicy && !requiresLayerContext ? "false" : "contextual")),
                Attribute("blocksMovement", anyBlocksMovement ? "true" : "false"),
                Attribute("elevationLevel", elevationLevel.ToString()),
                Attribute("renderBand", renderBand.ToString()),
                Attribute("colliderPolicy", colliderPolicy),
                Attribute("requiresLayerContext", requiresLayerContext ? "true" : "false"),
                Attribute("ruleVersion", "1")
            };
        }

        private static CustomTile.Attribute Attribute(string key, string value)
        {
            return new CustomTile.Attribute
            {
                key = key,
                value = value
            };
        }
    }
}
