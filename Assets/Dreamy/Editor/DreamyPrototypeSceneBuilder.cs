using Dreamy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dreamy.Editor
{
    public static class DreamyPrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Dreamy/Scenes/DreamyMobilePrototype.unity";

        [MenuItem("Dreamy/Build Mobile Prototype Scene")]
        public static void BuildMobilePrototypeScene()
        {
            EnsureFolders();
            ApplyMobileProjectSettings();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DreamyMobilePrototype";

            GameObject systems = new GameObject("Game Systems");
            systems.AddComponent<DreamyGameState>();

            Camera camera = CreateCamera();
            CreateWorld(camera);
            CreateHud();

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
            camera.backgroundColor = new Color(0.52f, 0.72f, 0.63f);
            camera.orthographic = true;
            camera.orthographicSize = 6.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.AddComponent<DreamyCameraFollow>();
            return camera;
        }

        private static void CreateWorld(Camera camera)
        {
            GameObject world = new GameObject("World");
            GameObject ground = new GameObject("Soft Meadow Background");
            ground.transform.SetParent(world.transform);
            SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
            groundRenderer.sprite = LoadSprite("Assets/Tiny Swords (Update 010)/Terrain/Ground/Tilemap_Flat.png");
            groundRenderer.color = new Color(0.82f, 0.94f, 0.72f);
            groundRenderer.sortingOrder = -10;
            ground.transform.localScale = new Vector3(0.06f, 0.06f, 1f);

            Transform marker = CreateTouchMarker(world.transform).transform;
            GameObject player = CreatePlayer(world.transform, marker);
            camera.GetComponent<DreamyCameraFollow>().Target = player.transform;

            CreateResource(world.transform, "Wood Node", DreamyResourceType.Wood, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree1.png", new Vector2(-2.7f, 2.2f));
            CreateResource(world.transform, "Gold Node", DreamyResourceType.Gold, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Gold/Gold Resource/Gold_Resource.png", new Vector2(2.6f, 1.6f));
            CreateResource(world.transform, "Food Node", DreamyResourceType.Food, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Meat/Meat Resource/Meat Resource.png", new Vector2(0f, -3.1f));

            for (int i = 0; i < 10; i++)
            {
                float x = -4.5f + i;
                CreateDecoration(world.transform, "Border Tree " + i, "Assets/Tiny Swords (Free Pack)/Terrain/Resources/Wood/Trees/Tree2.png", new Vector2(x, 4.7f), 0.65f, -2);
            }
        }

        private static GameObject CreatePlayer(Transform parent, Transform marker)
        {
            GameObject player = new GameObject("Player Pawn");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(0f, -0.5f, 0f);
            player.transform.localScale = Vector3.one * 0.8f;

            SpriteRenderer renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Tiny Swords (Free Pack)/Units/Yellow Units/Pawn/Pawn_Idle.png");
            renderer.sortingOrder = 10;

            CircleCollider2D collider = player.AddComponent<CircleCollider2D>();
            collider.radius = 0.32f;

            DreamyMobilePlayer controller = player.AddComponent<DreamyMobilePlayer>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("touchMarker").objectReferenceValue = marker;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static GameObject CreateTouchMarker(Transform parent)
        {
            GameObject marker = new GameObject("Tap Target Marker");
            marker.transform.SetParent(parent);
            marker.transform.position = new Vector3(0f, -0.5f, 0f);
            marker.transform.localScale = Vector3.one * 0.42f;
            marker.SetActive(false);

            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite("Assets/Tiny Swords (Update 010)/UI/Pointers/01.png");
            renderer.color = new Color(1f, 1f, 1f, 0.75f);
            renderer.sortingOrder = 6;
            return marker;
        }

        private static void CreateResource(Transform parent, string name, DreamyResourceType type, string spritePath, Vector2 position)
        {
            GameObject node = new GameObject(name);
            node.transform.SetParent(parent);
            node.transform.position = new Vector3(position.x, position.y, 0f);
            node.transform.localScale = Vector3.one * 0.7f;

            SpriteRenderer renderer = node.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spritePath);
            renderer.sortingOrder = 2;

            CircleCollider2D collider = node.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            DreamyResourceNode resource = node.AddComponent<DreamyResourceNode>();
            SerializedObject serialized = new SerializedObject(resource);
            serialized.FindProperty("resourceType").enumValueIndex = (int)type;
            serialized.FindProperty("amount").intValue = 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDecoration(Transform parent, string name, string spritePath, Vector2 position, float scale, int sortingOrder)
        {
            GameObject decoration = new GameObject(name);
            decoration.transform.SetParent(parent);
            decoration.transform.position = new Vector3(position.x, position.y, 0f);
            decoration.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = decoration.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spritePath);
            renderer.sortingOrder = sortingOrder;
        }

        private static void CreateHud()
        {
            GameObject canvasObject = new GameObject("Mobile HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1080f, 1920f);
            canvasObject.AddComponent<GraphicRaycaster>();
            canvasObject.AddComponent<DreamySafeArea>();

            GameObject topBar = CreatePanel(canvasObject.transform, "Resource Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -92f));
            Text wood = CreateText(topBar.transform, "Wood Text", "Wood 0", new Vector2(0.18f, 0.5f));
            Text gold = CreateText(topBar.transform, "Gold Text", "Gold 0", new Vector2(0.50f, 0.5f));
            Text food = CreateText(topBar.transform, "Food Text", "Food 0", new Vector2(0.82f, 0.5f));

            GameObject hint = CreatePanel(canvasObject.transform, "Touch Hint", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 132f));
            RectTransform hintRect = hint.GetComponent<RectTransform>();
            hintRect.sizeDelta = new Vector2(760f, 86f);
            Text hintText = CreateText(hint.transform, "Hint Text", "Tap anywhere to move. Walk near resources to collect.", new Vector2(0.5f, 0.5f));
            hintText.fontSize = 32;

            DreamyHud hud = canvasObject.AddComponent<DreamyHud>();
            hud.Bind(wood, gold, food);
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
            rect.sizeDelta = new Vector2(320f, 72f);
            return text;
        }

        private static Sprite LoadSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning("[Dreamy] Missing sprite: " + path);
            }

            return sprite;
        }
    }
}
