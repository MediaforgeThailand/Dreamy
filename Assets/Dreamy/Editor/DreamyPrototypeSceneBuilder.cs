using System.Collections.Generic;
using System.IO;
using Dreamy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace Dreamy.Editor
{
    public static class DreamyPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Dreamy/Scenes/DreamyMobilePrototype.unity";
        private const string Scene2Path = "Assets/Dreamy/Scenes/DreamyScene2.unity";
        private const string VillageScenePath = "Assets/Dreamy/Scenes/DreamyVillageMap.unity";
        private const string GeneratedAssetFolder = "Assets/Dreamy/Generated";
        private const string FirstSceneMapPath = "Assets/Dreamy/Maps/FirstScene/FirstSceneMap.png";
        private const string FirstSceneBlockedMarkupPath = "Assets/Dreamy/Maps/FirstScene/FirstSceneBlockedMarkup.png";
        private const string Scene2CompositeMapPath = "Assets/Dreamy/Maps/Scene2/map-composite.png";
        private const string VillageMapPath = "Assets/Dreamy/Maps/VillageMap/VillageMap.png";
        private const string ImportedMapPrefabPath = "Assets/Spritefusion/Maps/map.prefab";
        private const float PaintedMapPixelsPerUnit = 100f;
        private const int MarkupCollisionCellPixels = 96;
        private const int MarkupCollisionSampleStepPixels = 4;
        private const int MarkupCollisionMinimumBlueSamples = 10;
        private const int MarkupCollisionDilationCells = 1;
        private const float TileWorldSize = 0.64f;
        private static readonly Vector2 FallbackPlayerMinBounds = new Vector2(-3.2f, -5.0f);
        private static readonly Vector2 FallbackPlayerMaxBounds = new Vector2(3.2f, 5.0f);

        private readonly struct GridRect
        {
            public GridRect(int x, int y, int width, int height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }

            public int X { get; }
            public int Y { get; }
            public int Width { get; }
            public int Height { get; }
        }

        [MenuItem("Dreamy/Build Mobile Prototype Scene")]
        public static void BuildMobilePrototypeScene()
        {
            EnsureFolders();
            ApplyMobileProjectSettings();

            Sprite[] idleFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Idle.png",
                "PawnIdle",
                8);
            Sprite[] walkFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Run.png",
                "PawnRun",
                6);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DreamyMobilePrototype";

            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<DreamyGameState>();
            systems.AddComponent<DreamyMobileOrientation>();

            Camera camera = CreateCamera();
            DreamyVirtualJoystick joystick = CreateHud();
            CreateWorld(camera, joystick, idleFrames, walkFrames);
            ValidatePlayerFrames(idleFrames, walkFrames);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBuildSettingsScene(ScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Mobile prototype scene built at " + ScenePath);
        }

        [MenuItem("Dreamy/Build First Scene Map")]
        public static void BuildFirstSceneMap()
        {
            BuildMobilePrototypeScene();
        }

        [MenuItem("Dreamy/Build Scene 2 Map")]
        public static void BuildScene2Map()
        {
            EnsureFolders();
            ApplyMobileProjectSettings();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!File.Exists(ToAbsolutePath(Scene2CompositeMapPath)))
            {
                Debug.LogError("[Dreamy] Missing scene 2 composite map: " + Scene2CompositeMapPath);
                return;
            }

            Sprite[] idleFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Idle.png",
                "PawnIdle",
                8);
            Sprite[] walkFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Run.png",
                "PawnRun",
                6);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DreamyScene2";

            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<DreamyGameState>();
            systems.AddComponent<DreamyMobileOrientation>();

            Camera camera = CreateCamera();
            DreamyVirtualJoystick joystick = CreateHud();
            CreateScene2World(camera, joystick, idleFrames, walkFrames);
            ValidatePlayerFrames(idleFrames, walkFrames);

            EditorSceneManager.SaveScene(scene, Scene2Path);
            EnsureBuildSettingsScene(ScenePath);
            EnsureBuildSettingsScene(Scene2Path);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Scene 2 built at " + Scene2Path);
        }

        [MenuItem("Dreamy/Build Village Map")]
        public static void BuildVillageMap()
        {
            EnsureFolders();
            ApplyMobileProjectSettings();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            if (!File.Exists(ToAbsolutePath(VillageMapPath)))
            {
                Debug.LogError("[Dreamy] Missing village map: " + VillageMapPath);
                return;
            }

            Sprite[] idleFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Idle.png",
                "PawnIdle",
                8);
            Sprite[] walkFrames = CreateHorizontalFrameSprites(
                "Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Run.png",
                "PawnRun",
                6);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DreamyVillageMap";

            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<DreamyGameState>();
            systems.AddComponent<DreamyMobileOrientation>();

            Camera camera = CreateCamera();
            DreamyVirtualJoystick joystick = CreateHud();
            CreateVillageMapWorld(camera, joystick, idleFrames, walkFrames);
            ValidatePlayerFrames(idleFrames, walkFrames);

            EditorSceneManager.SaveScene(scene, VillageScenePath);
            EnsureBuildSettingsScene(ScenePath);
            EnsureBuildSettingsScene(VillageScenePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Village map built at " + VillageScenePath);
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Dreamy");
            CreateFolder("Assets/Dreamy", "Scenes");
            CreateFolder("Assets/Dreamy", "Scripts");
            CreateFolder("Assets/Dreamy", "Editor");
            CreateFolder("Assets/Dreamy", "Generated");
            CreateFolder("Assets/Dreamy", "Maps");
            CreateFolder("Assets/Dreamy/Maps", "FirstScene");
            CreateFolder("Assets/Dreamy/Maps", "Scene2");
            CreateFolder("Assets/Dreamy/Maps", "VillageMap");
        }

        private static void CreateFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                if (Directory.Exists(ToAbsolutePath(path)))
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    return;
                }

                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureBuildSettingsScene(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path == scenePath)
                {
                    scenes[i] = new EditorBuildSettingsScene(scenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                    return;
                }
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void ApplyMobileProjectSettings()
        {
            PlayerSettings.companyName = "Mediaforge Thailand";
            PlayerSettings.productName = "Dreamy";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.39f, 0.62f, 0.72f);
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<DreamyCameraFollow>();
            return camera;
        }

        private static void CreateWorld(Camera camera, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames)
        {
            GameObject world = new GameObject("World");
            GameObject levelRoot = new GameObject("Level Root");
            levelRoot.transform.SetParent(world.transform);

            bool usingPaintedMap = CreatePaintedFirstSceneMap(levelRoot.transform, out Bounds mapBounds);
            bool usingImportedMap = false;
            Vector2 playerStart = new Vector2(0f, -0.5f);
            Vector2 playerMinBounds = FallbackPlayerMinBounds;
            Vector2 playerMaxBounds = FallbackPlayerMaxBounds;

            if (usingPaintedMap)
            {
                playerStart = new Vector2(-1.2f, -0.4f);
                playerMinBounds = new Vector2(mapBounds.min.x + 0.8f, mapBounds.min.y + 0.8f);
                playerMaxBounds = new Vector2(mapBounds.max.x - 0.8f, mapBounds.max.y - 0.8f);
            }
            else
            {
                usingImportedMap = CreateImportedMap(levelRoot.transform, out mapBounds);
            }

            if (!usingPaintedMap && usingImportedMap)
            {
                playerStart = mapBounds.center;
                playerMinBounds = new Vector2(mapBounds.min.x + 0.4f, mapBounds.min.y + 0.4f);
                playerMaxBounds = new Vector2(mapBounds.max.x - 0.4f, mapBounds.max.y - 0.4f);
            }
            else if (!usingPaintedMap)
            {
                CreateWaterBorder(levelRoot.transform);
                CreateGroundGrid(levelRoot.transform);
                CreateLevelDecorations(levelRoot.transform);
            }

            if (!usingPaintedMap)
            {
                CreateResource(levelRoot.transform, "Wood Node", DreamyResourceType.Wood, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree1.png", playerStart + new Vector2(-2.35f, 2.55f), 0.72f);
                CreateResource(levelRoot.transform, "Gold Node", DreamyResourceType.Gold, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png", playerStart + new Vector2(2.35f, 1.45f), 0.78f);
                CreateResource(levelRoot.transform, "Food Node", DreamyResourceType.Food, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Meat/Meat Resource/Meat Resource.png", playerStart + new Vector2(-0.1f, -3.35f), 0.78f);
            }

            GameObject player = CreatePlayer(world.transform, joystick, idleFrames, walkFrames, playerStart, playerMinBounds, playerMaxBounds);
            DreamyCameraFollow follow = camera.GetComponent<DreamyCameraFollow>();
            if (usingPaintedMap)
            {
                camera.orthographicSize = 5.6f;
                follow.Configure(5.6f, false, false, 0.16f, 115f);
            }

            follow.Target = player.transform;
            follow.SetBounds(playerMinBounds, playerMaxBounds);
            camera.transform.position = new Vector3(playerStart.x, playerStart.y, camera.transform.position.z);
        }

        private static void CreateScene2World(Camera camera, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames)
        {
            GameObject world = new GameObject("World");
            GameObject levelRoot = new GameObject("Level Root");
            levelRoot.transform.SetParent(world.transform);

            if (!CreateScene2CompositeMap(levelRoot.transform, out Bounds mapBounds))
            {
                CreateWaterBorder(levelRoot.transform);
                CreateGroundGrid(levelRoot.transform);
                mapBounds = new Bounds(Vector3.zero, new Vector3(8f, 10f, 0f));
            }

            const float playerEdgeMargin = 0.5f;
            Vector2 playerMinBounds = new Vector2(mapBounds.min.x + playerEdgeMargin, mapBounds.min.y + playerEdgeMargin);
            Vector2 playerMaxBounds = new Vector2(mapBounds.max.x - playerEdgeMargin, mapBounds.max.y - playerEdgeMargin);
            Vector2 playerStart = new Vector2(mapBounds.center.x, mapBounds.center.y - mapBounds.extents.y * 0.22f);
            playerStart.x = Mathf.Clamp(playerStart.x, playerMinBounds.x, playerMaxBounds.x);
            playerStart.y = Mathf.Clamp(playerStart.y, playerMinBounds.y, playerMaxBounds.y);

            GameObject player = CreatePlayer(world.transform, joystick, idleFrames, walkFrames, playerStart, playerMinBounds, playerMaxBounds);
            DreamyCameraFollow follow = camera.GetComponent<DreamyCameraFollow>();
            float orthographicSize = CalculateScene2CameraSize(mapBounds);
            camera.orthographicSize = orthographicSize;
            follow.Configure(orthographicSize, false, false, 0.14f, 130f);
            follow.Target = player.transform;
            follow.SetBounds(new Vector2(mapBounds.min.x, mapBounds.min.y), new Vector2(mapBounds.max.x, mapBounds.max.y));
            camera.transform.position = new Vector3(playerStart.x, playerStart.y, camera.transform.position.z);
        }

        private static void CreateVillageMapWorld(Camera camera, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames)
        {
            GameObject world = new GameObject("World");
            GameObject levelRoot = new GameObject("Level Root");
            levelRoot.transform.SetParent(world.transform);

            if (!CreateSingleImageMap(levelRoot.transform, VillageMapPath, "Village Painted Map", out Bounds mapBounds))
            {
                CreateWaterBorder(levelRoot.transform);
                CreateGroundGrid(levelRoot.transform);
                mapBounds = new Bounds(Vector3.zero, new Vector3(8f, 10f, 0f));
            }

            const float playerEdgeMargin = 0.65f;
            Vector2 playerMinBounds = new Vector2(mapBounds.min.x + playerEdgeMargin, mapBounds.min.y + playerEdgeMargin);
            Vector2 playerMaxBounds = new Vector2(mapBounds.max.x - playerEdgeMargin, mapBounds.max.y - playerEdgeMargin);
            Vector2 playerStart = new Vector2(mapBounds.center.x - mapBounds.extents.x * 0.03f, mapBounds.center.y - mapBounds.extents.y * 0.10f);
            playerStart.x = Mathf.Clamp(playerStart.x, playerMinBounds.x, playerMaxBounds.x);
            playerStart.y = Mathf.Clamp(playerStart.y, playerMinBounds.y, playerMaxBounds.y);

            GameObject player = CreatePlayer(world.transform, joystick, idleFrames, walkFrames, playerStart, playerMinBounds, playerMaxBounds);
            DreamyCameraFollow follow = camera.GetComponent<DreamyCameraFollow>();
            float orthographicSize = CalculateVillageMapCameraSize(mapBounds);
            camera.orthographicSize = orthographicSize;
            follow.Configure(orthographicSize, false, false, 0.14f, 130f);
            follow.Target = player.transform;
            follow.SetBounds(new Vector2(mapBounds.min.x, mapBounds.min.y), new Vector2(mapBounds.max.x, mapBounds.max.y));
            camera.transform.position = new Vector3(playerStart.x, playerStart.y, camera.transform.position.z);
        }

        private static bool CreateScene2CompositeMap(Transform parent, out Bounds mapBounds)
        {
            mapBounds = new Bounds(Vector3.zero, Vector3.zero);
            if (!File.Exists(ToAbsolutePath(Scene2CompositeMapPath)))
            {
                return false;
            }

            ImportSprite(Scene2CompositeMapPath, PaintedMapPixelsPerUnit);
            Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Scene2CompositeMapPath);
            if (mapSprite == null)
            {
                Debug.LogWarning("[Dreamy] Could not load scene 2 map: " + Scene2CompositeMapPath);
                return false;
            }

            GameObject map = new GameObject("Scene 2 Composite Map");
            map.transform.SetParent(parent, false);
            SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
            renderer.sprite = mapSprite;
            renderer.sortingOrder = -100;

            mapBounds = renderer.bounds;
            Debug.Log("[Dreamy] Using scene 2 map at " + Scene2CompositeMapPath + " with size " + mapBounds.size);
            return true;
        }

        private static bool CreateSingleImageMap(Transform parent, string mapPath, string objectName, out Bounds mapBounds)
        {
            mapBounds = new Bounds(Vector3.zero, Vector3.zero);
            if (!File.Exists(ToAbsolutePath(mapPath)))
            {
                return false;
            }

            ImportPaintedMapSprite(mapPath, PaintedMapPixelsPerUnit);
            Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(mapPath);
            if (mapSprite == null)
            {
                Debug.LogWarning("[Dreamy] Could not load single image map: " + mapPath);
                return false;
            }

            GameObject map = new GameObject(objectName);
            map.transform.SetParent(parent, false);
            SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
            renderer.sprite = mapSprite;
            renderer.sortingOrder = -100;

            mapBounds = renderer.bounds;
            Debug.Log("[Dreamy] Using single image map at " + mapPath + " with size " + mapBounds.size);
            return true;
        }

        private static float CalculateScene2CameraSize(Bounds mapBounds)
        {
            const float wideMobileLandscapeAspect = 20f / 9f;
            float sizeByHeight = mapBounds.extents.y * 0.86f;
            float sizeByWidth = mapBounds.extents.x / wideMobileLandscapeAspect * 0.96f;
            return Mathf.Clamp(Mathf.Min(sizeByHeight, sizeByWidth), 2.8f, 4.4f);
        }

        private static float CalculateVillageMapCameraSize(Bounds mapBounds)
        {
            const float wideMobileLandscapeAspect = 20f / 9f;
            float sizeByHeight = mapBounds.extents.y * 0.48f;
            float sizeByWidth = mapBounds.extents.x / wideMobileLandscapeAspect * 0.62f;
            return Mathf.Clamp(Mathf.Min(sizeByHeight, sizeByWidth), 4.8f, 6.2f);
        }

        private static bool CreatePaintedFirstSceneMap(Transform parent, out Bounds mapBounds)
        {
            mapBounds = new Bounds(Vector3.zero, Vector3.zero);
            if (!File.Exists(ToAbsolutePath(FirstSceneMapPath)))
            {
                return false;
            }

            ImportPaintedMapSprite(FirstSceneMapPath, PaintedMapPixelsPerUnit);
            ImportMarkupTexture(FirstSceneBlockedMarkupPath);

            Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>(FirstSceneMapPath);
            if (mapSprite == null)
            {
                Debug.LogWarning("[Dreamy] Could not load painted first-scene map: " + FirstSceneMapPath);
                return false;
            }

            GameObject map = new GameObject("First Scene Painted Map");
            map.transform.SetParent(parent, false);
            SpriteRenderer renderer = map.AddComponent<SpriteRenderer>();
            renderer.sprite = mapSprite;
            renderer.sortingOrder = -100;

            mapBounds = renderer.bounds;
            CreateMarkupCollision(parent);
            Debug.Log("[Dreamy] Using painted first scene map at " + FirstSceneMapPath + " with size " + mapBounds.size);
            return true;
        }

        private static void CreateMarkupCollision(Transform parent)
        {
            if (!File.Exists(ToAbsolutePath(FirstSceneBlockedMarkupPath)))
            {
                Debug.LogWarning("[Dreamy] Missing blocked-area markup: " + FirstSceneBlockedMarkupPath);
                return;
            }

            Texture2D markup = LoadTextureFromProject(FirstSceneBlockedMarkupPath);
            if (markup == null)
            {
                return;
            }

            List<GridRect> blockedRects = BuildBlockedRectsFromMarkup(markup);
            GameObject blockerRoot = new GameObject("Non Walkable Markup Colliders");
            blockerRoot.transform.SetParent(parent, false);
            blockerRoot.isStatic = true;

            for (int i = 0; i < blockedRects.Count; i++)
            {
                GridRect rect = blockedRects[i];
                GameObject blocker = new GameObject($"Blocked Area {i + 1:00}");
                blocker.transform.SetParent(blockerRoot.transform, false);
                blocker.isStatic = true;

                float pixelMinX = rect.X * MarkupCollisionCellPixels;
                float pixelMinY = rect.Y * MarkupCollisionCellPixels;
                float pixelWidth = rect.Width * MarkupCollisionCellPixels;
                float pixelHeight = rect.Height * MarkupCollisionCellPixels;

                float centerX = (pixelMinX + pixelWidth * 0.5f - markup.width * 0.5f) / PaintedMapPixelsPerUnit;
                float centerY = (pixelMinY + pixelHeight * 0.5f - markup.height * 0.5f) / PaintedMapPixelsPerUnit;
                blocker.transform.localPosition = new Vector3(centerX, centerY, 0f);

                BoxCollider2D collider = blocker.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(pixelWidth / PaintedMapPixelsPerUnit, pixelHeight / PaintedMapPixelsPerUnit);
                collider.isTrigger = false;
            }

            UnityEngine.Object.DestroyImmediate(markup);
            Debug.Log($"[Dreamy] Created {blockedRects.Count} non-walkable colliders from {FirstSceneBlockedMarkupPath}");
        }

        private static List<GridRect> BuildBlockedRectsFromMarkup(Texture2D markup)
        {
            int columns = Mathf.CeilToInt(markup.width / (float)MarkupCollisionCellPixels);
            int rows = Mathf.CeilToInt(markup.height / (float)MarkupCollisionCellPixels);
            bool[,] blockedCells = new bool[columns, rows];
            Color32[] pixels = markup.GetPixels32();

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    int blueSamples = CountBlueMarkupSamples(pixels, markup.width, markup.height, x, y);
                    blockedCells[x, y] = blueSamples >= MarkupCollisionMinimumBlueSamples;
                }
            }

            blockedCells = DilateBlockedCells(blockedCells, columns, rows, MarkupCollisionDilationCells);
            return MergeBlockedCells(blockedCells, columns, rows);
        }

        private static int CountBlueMarkupSamples(Color32[] pixels, int textureWidth, int textureHeight, int cellX, int cellY)
        {
            int startX = cellX * MarkupCollisionCellPixels;
            int startY = cellY * MarkupCollisionCellPixels;
            int endX = Mathf.Min(startX + MarkupCollisionCellPixels, textureWidth);
            int endY = Mathf.Min(startY + MarkupCollisionCellPixels, textureHeight);
            int blueSamples = 0;

            for (int y = startY; y < endY; y += MarkupCollisionSampleStepPixels)
            {
                int rowOffset = y * textureWidth;
                for (int x = startX; x < endX; x += MarkupCollisionSampleStepPixels)
                {
                    if (IsMarkupBlue(pixels[rowOffset + x]))
                    {
                        blueSamples++;
                    }
                }
            }

            return blueSamples;
        }

        private static bool IsMarkupBlue(Color32 pixel)
        {
            return pixel.a > 64
                && pixel.r < 80
                && pixel.g > 70
                && pixel.g < 170
                && pixel.b > 170
                && pixel.b - pixel.g > 35
                && pixel.b - pixel.r > 110;
        }

        private static bool[,] DilateBlockedCells(bool[,] source, int columns, int rows, int iterations)
        {
            bool[,] result = source;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                bool[,] expanded = new bool[columns, rows];
                for (int y = 0; y < rows; y++)
                {
                    for (int x = 0; x < columns; x++)
                    {
                        if (!result[x, y])
                        {
                            continue;
                        }

                        for (int offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            for (int offsetX = -1; offsetX <= 1; offsetX++)
                            {
                                int targetX = x + offsetX;
                                int targetY = y + offsetY;
                                if (targetX >= 0 && targetX < columns && targetY >= 0 && targetY < rows)
                                {
                                    expanded[targetX, targetY] = true;
                                }
                            }
                        }
                    }
                }

                result = expanded;
            }

            return result;
        }

        private static List<GridRect> MergeBlockedCells(bool[,] blockedCells, int columns, int rows)
        {
            List<GridRect> rects = new List<GridRect>();
            bool[,] used = new bool[columns, rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    if (!blockedCells[x, y] || used[x, y])
                    {
                        continue;
                    }

                    int width = 1;
                    while (x + width < columns && blockedCells[x + width, y] && !used[x + width, y])
                    {
                        width++;
                    }

                    int height = 1;
                    bool canGrow = true;
                    while (y + height < rows && canGrow)
                    {
                        for (int testX = x; testX < x + width; testX++)
                        {
                            if (!blockedCells[testX, y + height] || used[testX, y + height])
                            {
                                canGrow = false;
                                break;
                            }
                        }

                        if (canGrow)
                        {
                            height++;
                        }
                    }

                    for (int markY = y; markY < y + height; markY++)
                    {
                        for (int markX = x; markX < x + width; markX++)
                        {
                            used[markX, markY] = true;
                        }
                    }

                    rects.Add(new GridRect(x, y, width, height));
                }
            }

            return rects;
        }

        private static bool CreateImportedMap(Transform parent, out Bounds mapBounds)
        {
            mapBounds = new Bounds(Vector3.zero, Vector3.zero);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ImportedMapPrefabPath);
            if (prefab == null)
            {
                return false;
            }

            GameObject map = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (map == null)
            {
                Debug.LogWarning("[Dreamy] Could not instantiate imported map prefab: " + ImportedMapPrefabPath);
                return false;
            }

            map.name = "Spritefusion Imported Map";
            map.transform.SetParent(parent, false);
            map.transform.localPosition = Vector3.zero;
            map.transform.localRotation = Quaternion.identity;
            map.transform.localScale = Vector3.one;
            PrepareImportedMapForRuntime(map);

            mapBounds = CalculateRendererBounds(map);
            if (mapBounds.size == Vector3.zero)
            {
                Debug.LogWarning("[Dreamy] Imported map has no renderer bounds: " + ImportedMapPrefabPath);
                return true;
            }

            Vector3 offset = -mapBounds.center;
            map.transform.position += offset;
            mapBounds.center += offset;
            Debug.Log("[Dreamy] Using imported Spritefusion map at " + ImportedMapPrefabPath + " with size " + mapBounds.size);
            return true;
        }

        private static void PrepareImportedMapForRuntime(GameObject map)
        {
            TilemapRenderer[] tilemapRenderers = map.GetComponentsInChildren<TilemapRenderer>();
            for (int i = 0; i < tilemapRenderers.Length; i++)
            {
                tilemapRenderers[i].mode = TilemapRenderer.Mode.Individual;
            }

            TilemapCollider2D[] tilemapColliders = map.GetComponentsInChildren<TilemapCollider2D>();
            for (int i = 0; i < tilemapColliders.Length; i++)
            {
                bool shouldBlockMovement = DreamyLevelTileRules.LayerBlocksMovement(tilemapColliders[i].gameObject.name);
                tilemapColliders[i].enabled = shouldBlockMovement;
                tilemapColliders[i].isTrigger = false;

                CompositeCollider2D composite = tilemapColliders[i].GetComponent<CompositeCollider2D>();
                if (composite != null)
                {
                    composite.enabled = shouldBlockMovement;
                    composite.isTrigger = false;
                }
            }
        }

        private static Bounds CalculateRendererBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return new Bounds(root.transform.position, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void CreateWaterBorder(Transform parent)
        {
            Sprite water = LoadSprite("Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Water Background color.png");
            Sprite foam = LoadSprite("Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Water Foam.png");

            CreateTiledSprite(parent, "North Water", water, new Vector2(0f, 6.25f), new Vector2(8.8f, 1.6f), -40);
            CreateTiledSprite(parent, "South Water", water, new Vector2(0f, -6.25f), new Vector2(8.8f, 1.6f), -40);
            CreateTiledSprite(parent, "West Water", water, new Vector2(-4.9f, 0f), new Vector2(1.8f, 12.5f), -40);
            CreateTiledSprite(parent, "East Water", water, new Vector2(4.9f, 0f), new Vector2(1.8f, 12.5f), -40);

            CreateTiledSprite(parent, "North Foam", foam, new Vector2(0f, 5.42f), new Vector2(7.2f, 0.5f), -30);
            CreateTiledSprite(parent, "South Foam", foam, new Vector2(0f, -5.42f), new Vector2(7.2f, 0.5f), -30);
        }

        private static void CreateGroundGrid(Transform parent)
        {
            Sprite grass = CreateTileSpriteFromAtlas(
                "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color1.png",
                GeneratedAssetFolder + "/DreamyGrassTile.png",
                1,
                1,
                64);
            Sprite path = CreateTileSpriteFromAtlas(
                "Assets/Tiny Swords (Free Pack)/Terrain/Tileset/Tilemap_color5.png",
                GeneratedAssetFolder + "/DreamyPathTile.png",
                1,
                1,
                64);

            GameObject groundRoot = new GameObject("Walkable Meadow");
            groundRoot.transform.SetParent(parent);

            const int columns = 11;
            const int rows = 17;
            int centerColumn = columns / 2;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    bool isPath = Mathf.Abs(x - centerColumn) <= 1 || (y == 5 && x >= 2 && x <= 8) || (y == 11 && x >= 1 && x <= 6);
                    Sprite sprite = isPath && path != null ? path : grass;
                    string tileName = isPath ? "Path Tile" : "Grass Tile";
                    Vector2 position = new Vector2(
                        (x - (columns - 1) * 0.5f) * TileWorldSize,
                        (y - (rows - 1) * 0.5f) * TileWorldSize);

                    CreateTile(groundRoot.transform, tileName, sprite, position);
                }
            }
        }

        private static void CreateLevelDecorations(Transform parent)
        {
            CreateDecoration(parent, "Northwest Tree", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree2.png", new Vector2(-3.15f, 3.95f), 0.68f);
            CreateDecoration(parent, "North Tree", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree3.png", new Vector2(-0.65f, 4.25f), 0.7f);
            CreateDecoration(parent, "Northeast Tree", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree4.png", new Vector2(3.05f, 3.75f), 0.68f);
            CreateDecoration(parent, "West Stump", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Stump 1.png", new Vector2(-3.25f, -1.7f), 0.7f);
            CreateDecoration(parent, "East Stump", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Stump 4.png", new Vector2(3.2f, -2.25f), 0.72f);
            CreateDecoration(parent, "River Rock 1", "Assets/Tiny Swords (Free Pack)/Terrain/Decorations/Rocks in the Water/Water Rocks_01.png", new Vector2(-3.75f, 4.95f), 0.72f);
            CreateDecoration(parent, "River Rock 2", "Assets/Tiny Swords (Free Pack)/Terrain/Decorations/Rocks in the Water/Water Rocks_03.png", new Vector2(3.85f, -4.95f), 0.72f);
            CreateDecoration(parent, "Sheep", "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Meat/Sheep/Sheep_Idle.png", new Vector2(2.45f, -3.1f), 0.6f);
        }

        private static GameObject CreatePlayer(Transform parent, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames, Vector2 startPosition, Vector2 minBounds, Vector2 maxBounds)
        {
            GameObject player = new GameObject("Player Pawn");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(startPosition.x, startPosition.y, 0f);
            player.transform.localScale = Vector3.one;

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
            renderer.sortingOrder = 11;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.32f;

            player.AddComponent<DreamyCharacterStats>();
            player.AddComponent<DreamyInventory>();
            player.AddComponent<DreamyExperience>();

            DreamyMobilePlayer controller = player.AddComponent<DreamyMobilePlayer>();
            controller.Bind(joystick, idleFrames, walkFrames);
            controller.SetMovementBounds(minBounds, maxBounds);
            return player;
        }

        private static void CreateResource(Transform parent, string name, DreamyResourceType type, string spritePath, Vector2 position, float scale)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent);
            node.transform.position = new Vector3(position.x, position.y, 0f);
            node.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spritePath);
            renderer.sortingOrder = SortingOrderFor(position);

            CircleCollider2D collider = node.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            DreamyResourceNode resource = node.AddComponent<DreamyResourceNode>();
            SerializedObject serialized = new SerializedObject(resource);
            serialized.FindProperty("resourceType").enumValueIndex = (int)type;
            serialized.FindProperty("amount").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDecoration(Transform parent, string name, string spritePath, Vector2 position, float scale)
        {
            GameObject decoration = new GameObject(name);
            decoration.transform.SetParent(parent);
            decoration.transform.position = new Vector3(position.x, position.y, 0f);
            decoration.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spritePath);
            renderer.sortingOrder = SortingOrderFor(position);
        }

        private static void CreateTile(Transform parent, string name, Sprite sprite, Vector2 position)
        {
            GameObject tile = new GameObject(name);
            tile.transform.SetParent(parent);
            tile.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -20;
        }

        private static void CreateTiledSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, int sortingOrder)
        {
            GameObject tile = new GameObject(name);
            tile.transform.SetParent(parent);
            tile.transform.position = new Vector3(position.x, position.y, 0f);

            SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;

            if (sprite != null && sprite.bounds.size.x > 0f && sprite.bounds.size.y > 0f)
            {
                tile.transform.localScale = new Vector3(size.x / sprite.bounds.size.x, size.y / sprite.bounds.size.y, 1f);
            }
        }

        private static int SortingOrderFor(Vector2 position)
        {
            return 100 - Mathf.RoundToInt(position.y * 10f);
        }

        private static DreamyVirtualJoystick CreateHud()
        {
            CreateEventSystem();

            GameObject canvasObject = new GameObject("Mobile HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<DreamySafeArea>();

            GameObject topBar = CreatePanel(canvasObject.transform, "Resource Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -60f));
            Text wood = CreateText(topBar.transform, "Wood Text", "Wood 0", new Vector2(0.18f, 0.5f));
            Text gold = CreateText(topBar.transform, "Gold Text", "Gold 0", new Vector2(0.50f, 0.5f));
            Text food = CreateText(topBar.transform, "Food Text", "Food 0", new Vector2(0.82f, 0.5f));

            DreamyVirtualJoystick joystick = CreateJoystick(canvasObject.transform);

            DreamyHud hud = canvasObject.AddComponent<DreamyHud>();
            hud.Bind(wood, gold, food);
            return joystick;
        }

        private static void ValidatePlayerFrames(Sprite[] idleFrames, Sprite[] walkFrames)
        {
            if (idleFrames == null || idleFrames.Length != 8 || walkFrames == null || walkFrames.Length != 6)
            {
                Debug.LogWarning("[Dreamy] Player animation frames were not generated as expected.");
                return;
            }

            bool hasWideSheet = false;
            for (int i = 0; i < idleFrames.Length; i++)
            {
                hasWideSheet |= idleFrames[i] != null && idleFrames[i].texture.width > 256;
            }

            for (int i = 0; i < walkFrames.Length; i++)
            {
                hasWideSheet |= walkFrames[i] != null && walkFrames[i].texture.width > 256;
            }

            if (hasWideSheet)
            {
                Debug.LogWarning("[Dreamy] Player is using a full sprite sheet instead of cropped animation frames.");
            }
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static DreamyVirtualJoystick CreateJoystick(Transform parent)
        {
            GameObject joystickRoot = new GameObject("Virtual Joystick");
            joystickRoot.transform.SetParent(parent, false);
            Image rootImage = joystickRoot.AddComponent<Image>();
            rootImage.sprite = LoadSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/BigBlueButton_Regular.png");
            rootImage.color = new Color(1f, 1f, 1f, 0f);
            rootImage.raycastTarget = true;

            RectTransform rootRect = joystickRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(170f, 155f);
            rootRect.sizeDelta = new Vector2(220f, 220f);

            GameObject handle = new GameObject("Joystick Handle");
            handle.transform.SetParent(joystickRoot.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = LoadSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
            handleImage.color = new Color(1f, 1f, 1f, 0.94f);
            handleImage.raycastTarget = false;
            handleImage.preserveAspect = true;

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(92f, 92f);

            DreamyVirtualJoystick joystick = joystickRoot.AddComponent<DreamyVirtualJoystick>();
            joystick.Bind(handleRect, 82f);
            return joystick;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.08f, 0.10f, 0.12f, 0.72f);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(0f, 76f);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchor)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 32;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(320f, 60f);
            return text;
        }

        private static Sprite[] CreateHorizontalFrameSprites(string sourcePath, string outputPrefix, int frameCount)
        {
            Texture2D source = LoadTextureFromProject(sourcePath);
            if (source == null || frameCount <= 0)
            {
                return new Sprite[0];
            }

            Sprite[] sprites = new Sprite[frameCount];
            int frameWidth = source.width / frameCount;

            for (int i = 0; i < frameCount; i++)
            {
                string outputPath = $"{GeneratedAssetFolder}/{outputPrefix}_{i:00}.png";
                WriteCroppedTexture(source, outputPath, i * frameWidth, 0, frameWidth, source.height);
                ImportSprite(outputPath, 100f);
                sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
            }

            UnityEngine.Object.DestroyImmediate(source);
            return sprites;
        }

        private static Sprite CreateTileSpriteFromAtlas(string sourcePath, string outputPath, int column, int rowFromTop, int tileSize)
        {
            Texture2D source = LoadTextureFromProject(sourcePath);
            if (source == null)
            {
                return null;
            }

            int x = Mathf.Clamp(column * tileSize, 0, source.width - tileSize);
            int y = Mathf.Clamp(source.height - ((rowFromTop + 1) * tileSize), 0, source.height - tileSize);
            WriteCroppedTexture(source, outputPath, x, y, tileSize, tileSize);
            ImportSprite(outputPath, 100f);
            UnityEngine.Object.DestroyImmediate(source);
            return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }

        private static Texture2D LoadTextureFromProject(string assetPath)
        {
            string fullPath = ToAbsolutePath(assetPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning("[Dreamy] Missing texture: " + assetPath);
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(File.ReadAllBytes(fullPath)))
            {
                Debug.LogWarning("[Dreamy] Could not load texture: " + assetPath);
                UnityEngine.Object.DestroyImmediate(texture);
                return null;
            }

            return texture;
        }

        private static void WriteCroppedTexture(Texture2D source, string outputPath, int x, int y, int width, int height)
        {
            Texture2D cropped = new Texture2D(width, height, TextureFormat.RGBA32, false);
            cropped.SetPixels(source.GetPixels(x, y, width, height));
            cropped.Apply();

            string fullOutputPath = ToAbsolutePath(outputPath);
            string directory = Path.GetDirectoryName(fullOutputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(fullOutputPath, cropped.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(cropped);
        }

        private static Sprite LoadSprite(string path)
        {
            ImportSprite(path, 100f);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[Dreamy] Missing sprite: " + path);
            }

            return sprite;
        }

        private static void ImportPaintedMapSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (importer.maxTextureSize != 4096)
            {
                importer.maxTextureSize = 4096;
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

        private static void ImportMarkupTexture(string path)
        {
            if (!File.Exists(ToAbsolutePath(path)))
            {
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Default)
            {
                importer.textureType = TextureImporterType.Default;
                changed = true;
            }

            if (!importer.isReadable)
            {
                importer.isReadable = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
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

        private static void ImportSprite(string path, float pixelsPerUnit)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
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

        private static string ToAbsolutePath(string assetPath)
        {
            string normalizedPath = assetPath.Replace("\\", "/");
            if (normalizedPath.StartsWith("Assets/"))
            {
                normalizedPath = normalizedPath.Substring("Assets/".Length);
            }

            return Path.Combine(Application.dataPath, normalizedPath.Replace("/", Path.DirectorySeparatorChar.ToString()));
        }
    }
}
