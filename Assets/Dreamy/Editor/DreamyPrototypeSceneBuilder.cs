using System.IO;
using Dreamy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dreamy.Editor
{
    public static class DreamyPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Dreamy/Scenes/DreamyMobilePrototype.unity";
        private const string GeneratedAssetFolder = "Assets/Dreamy/Generated";
        private const float TileWorldSize = 0.64f;
        private static readonly Vector2 PlayerMinBounds = new Vector2(-3.2f, -5.0f);
        private static readonly Vector2 PlayerMaxBounds = new Vector2(3.2f, 5.0f);

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

            Camera camera = CreateCamera();
            DreamyVirtualJoystick joystick = CreateHud();
            CreateWorld(camera, joystick, idleFrames, walkFrames);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Mobile prototype scene built at " + ScenePath);
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "Dreamy");
            CreateFolder("Assets/Dreamy", "Scenes");
            CreateFolder("Assets/Dreamy", "Scripts");
            CreateFolder("Assets/Dreamy", "Editor");
            CreateFolder("Assets/Dreamy", "Generated");
        }

        private static void CreateFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void ApplyMobileProjectSettings()
        {
            PlayerSettings.companyName = "Mediaforge Thailand";
            PlayerSettings.productName = "Dreamy";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
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
            camera.orthographicSize = 5.9f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<DreamyCameraFollow>();
            return camera;
        }

        private static void CreateWorld(Camera camera, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames)
        {
            GameObject world = new GameObject("World");
            GameObject levelRoot = new GameObject("Level Root");
            levelRoot.transform.SetParent(world.transform);

            CreateWaterBorder(levelRoot.transform);
            CreateGroundGrid(levelRoot.transform);
            CreateLevelDecorations(levelRoot.transform);

            CreateResource(levelRoot.transform, "Wood Node", DreamyResourceType.Wood, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree1.png", new Vector2(-2.35f, 2.55f), 0.72f);
            CreateResource(levelRoot.transform, "Gold Node", DreamyResourceType.Gold, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png", new Vector2(2.35f, 1.45f), 0.78f);
            CreateResource(levelRoot.transform, "Food Node", DreamyResourceType.Food, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Meat/Meat Resource/Meat Resource.png", new Vector2(-0.1f, -3.35f), 0.78f);

            GameObject player = CreatePlayer(world.transform, joystick, idleFrames, walkFrames);
            DreamyCameraFollow follow = camera.GetComponent<DreamyCameraFollow>();
            follow.Target = player.transform;
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

        private static GameObject CreatePlayer(Transform parent, DreamyVirtualJoystick joystick, Sprite[] idleFrames, Sprite[] walkFrames)
        {
            GameObject player = new GameObject("Player Pawn");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, -0.5f, 0f);
            player.transform.localScale = Vector3.one * 0.82f;

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = idleFrames != null && idleFrames.Length > 0 ? idleFrames[0] : null;
            renderer.sortingOrder = 100;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.32f;

            DreamyMobilePlayer controller = player.AddComponent<DreamyMobilePlayer>();
            controller.Bind(joystick, idleFrames, walkFrames);
            controller.SetMovementBounds(PlayerMinBounds, PlayerMaxBounds);
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
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<DreamySafeArea>();

            GameObject topBar = CreatePanel(canvasObject.transform, "Resource Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f));
            Text wood = CreateText(topBar.transform, "Wood Text", "Wood 0", new Vector2(0.18f, 0.5f));
            Text gold = CreateText(topBar.transform, "Gold Text", "Gold 0", new Vector2(0.50f, 0.5f));
            Text food = CreateText(topBar.transform, "Food Text", "Food 0", new Vector2(0.82f, 0.5f));

            GameObject hint = CreatePanel(canvasObject.transform, "Touch Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 132f));
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(780f, 86f);
            Text hintText = CreateText(hint.transform, "Hint Text", "Drag joystick to walk. Collect nearby resources.", new Vector2(0.5f, 0.5f));
            hintText.fontSize = 32;

            DreamyVirtualJoystick joystick = CreateJoystick(canvasObject.transform);

            DreamyHud hud = canvasObject.AddComponent<DreamyHud>();
            hud.Bind(wood, gold, food);
            return joystick;
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
            rootImage.color = new Color(1f, 1f, 1f, 0.5f);

            RectTransform rootRect = joystickRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(190f, 210f);
            rootRect.sizeDelta = new Vector2(250f, 250f);

            GameObject handle = new GameObject("Joystick Handle");
            handle.transform.SetParent(joystickRoot.transform, false);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.sprite = LoadSprite("Assets/Tiny Swords (Free Pack)/UI Elements/UI Elements/Buttons/SmallBlueRoundButton_Regular.png");
            handleImage.color = new Color(1f, 1f, 1f, 0.86f);
            handleImage.raycastTarget = false;

            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0.5f);
            handleRect.anchorMax = new Vector2(0.5f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(104f, 104f);

            DreamyVirtualJoystick joystick = joystickRoot.AddComponent<DreamyVirtualJoystick>();
            joystick.Bind(handleRect, 92f);
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
            rect.sizeDelta = new Vector2(0f, 92f);
            return panel;
        }

        private static Text CreateText(Transform parent, string name, string value, Vector2 anchor)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 38;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(340f, 72f);
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
