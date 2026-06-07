using Dreamy.Extraction;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dreamy.Editor
{
    public static class ExtractionPrototypeSceneBuilder
    {
        public const string BaseScenePath = "Assets/Dreamy/Scenes/Prototype/Prototype_Base.unity";
        public const string RunScenePath = "Assets/Dreamy/Scenes/Prototype/Prototype_Run.unity";

        private const string GeneratedFolder = "Assets/Dreamy/Generated/ExtractionPrototype";

        [MenuItem("Dreamy/Extraction/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureFolders();
            PrototypeContent content = EnsurePrototypeContent();
            BuildBaseScene();
            BuildRunScene(content);
            EnsureBuildSettingsScene(BaseScenePath);
            EnsureBuildSettingsScene(RunScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Dreamy] Extraction prototype scenes built: " + BaseScenePath + " and " + RunScenePath);
        }

        private static void BuildBaseScene()
        {
            Scene scene = CreateGeneratedScene("Prototype_Base", out Scene previousScene, out bool closeAfterSave);
            scene.name = "Prototype_Base";

            CreateCamera(new Color(0.1f, 0.13f, 0.16f), 5.5f);
            CreateEventSystem();
            CreateSessionObject();
            CreateBaseControllerAndUi();
            CreateBasePlaceholderObjects();

            SaveAndCloseGeneratedScene(scene, BaseScenePath, previousScene, closeAfterSave);
        }

        private static void BuildRunScene(PrototypeContent content)
        {
            Scene scene = CreateGeneratedScene("Prototype_Run", out Scene previousScene, out bool closeAfterSave);
            scene.name = "Prototype_Run";

            CreateCamera(new Color(0.12f, 0.16f, 0.18f), 6f);
            CreateSessionObject();

            GameObject systems = new GameObject("Extraction Prototype Systems");
            systems.AddComponent<DreamyMobileOrientation>();
            ExtractionPrototypeBootstrap bootstrap = systems.AddComponent<ExtractionPrototypeBootstrap>();
            bootstrap.Configure(content.MapData, content.WeaponData, content.EnemyData);

            SaveAndCloseGeneratedScene(scene, RunScenePath, previousScene, closeAfterSave);
        }

        private static Scene CreateGeneratedScene(string sceneName, out Scene previousScene, out bool closeAfterSave)
        {
            previousScene = SceneManager.GetActiveScene();
            bool canCreateAdditive = previousScene.IsValid() && !string.IsNullOrEmpty(previousScene.path);
            closeAfterSave = canCreateAdditive;
            Scene scene = EditorSceneManager.NewScene(
                NewSceneSetup.EmptyScene,
                canCreateAdditive ? NewSceneMode.Additive : NewSceneMode.Single);
            scene.name = sceneName;
            EditorSceneManager.SetActiveScene(scene);
            return scene;
        }

        private static void CreateCamera(Color backgroundColor, float orthographicSize)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void CreateSessionObject()
        {
            GameObject session = new GameObject("Extraction Game Session");
            session.AddComponent<ExtractionGameSession>();
            session.AddComponent<ExtractionMapUnlockService>();
            session.AddComponent<ExtractionRecoveryService>();
        }

        private static void CreateBaseControllerAndUi()
        {
            GameObject controllerObject = new GameObject("Extraction Base Controller");
            ExtractionBaseSceneController controller = controllerObject.AddComponent<ExtractionBaseSceneController>();

            GameObject canvasObject = new GameObject("Prototype Base HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            Text title = CreateText(canvasObject.transform, "Mini World Extraction RPG", 42, TextAnchor.UpperLeft);
            RectTransform titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(0f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(64f, -56f);
            titleRect.sizeDelta = new Vector2(760f, 80f);

            Text storageText = CreateText(canvasObject.transform, string.Empty, 26, TextAnchor.UpperLeft);
            RectTransform storageRect = storageText.GetComponent<RectTransform>();
            storageRect.anchorMin = new Vector2(0f, 1f);
            storageRect.anchorMax = new Vector2(0f, 1f);
            storageRect.pivot = new Vector2(0f, 1f);
            storageRect.anchoredPosition = new Vector2(64f, -145f);
            storageRect.sizeDelta = new Vector2(720f, 360f);

            Button startButton = CreateButton(canvasObject.transform, "Start Expedition", new Vector2(64f, 64f), new Vector2(360f, 96f), new Vector2(0f, 0f));
            UnityEventTools.AddPersistentListener(startButton.onClick, controller.StartExpedition);
            controller.Bind(storageText, "Prototype_Run");
        }

        private static void CreateBasePlaceholderObjects()
        {
            CreateLabelledWorldMarker("Storage", new Vector2(-3f, 0.8f), new Color(0.35f, 0.7f, 1f, 1f));
            CreateLabelledWorldMarker("Workshop", new Vector2(0f, 0.8f), new Color(0.95f, 0.75f, 0.25f, 1f));
            CreateLabelledWorldMarker("Recovery Office", new Vector2(3f, 0.8f), new Color(0.75f, 0.55f, 1f, 1f));
        }

        private static void CreateLabelledWorldMarker(string label, Vector2 position, Color color)
        {
            GameObject marker = new GameObject(label + " Placeholder");
            marker.transform.position = position;
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = ExtractionPlaceholderSprite.Get(color);
            renderer.sortingOrder = 1;
        }

        private static Text CreateText(Transform parent, string textValue, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.text = textValue;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 position, Vector2 size, Vector2 anchor)
        {
            GameObject buttonObject = new GameObject(label + " Button");
            buttonObject.transform.SetParent(parent, false);
            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.1f, 0.3f, 0.45f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, label, 28, TextAnchor.MiddleCenter);
            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void CreateEventSystem()
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static void SaveAndCloseGeneratedScene(Scene scene, string path, Scene previousScene, bool closeAfterSave)
        {
            EditorSceneManager.SaveScene(scene, path);
            if (!closeAfterSave)
            {
                return;
            }

            EditorSceneManager.CloseScene(scene, true);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                EditorSceneManager.SetActiveScene(previousScene);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Dreamy");
            EnsureFolder("Assets/Dreamy/Scenes");
            EnsureFolder("Assets/Dreamy/Scenes/Prototype");
            EnsureFolder("Assets/Dreamy/Generated");
            EnsureFolder(GeneratedFolder);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int slash = folderPath.LastIndexOf('/');
            string parent = folderPath.Substring(0, slash);
            string folderName = folderPath.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void EnsureBuildSettingsScene(string scenePath)
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].path == scenePath)
                {
                    existing[i].enabled = true;
                    EditorBuildSettings.scenes = existing;
                    return;
                }
            }

            EditorBuildSettingsScene[] next = new EditorBuildSettingsScene[existing.Length + 1];
            for (int i = 0; i < existing.Length; i++)
            {
                next[i] = existing[i];
            }

            next[next.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = next;
        }

        private static PrototypeContent EnsurePrototypeContent()
        {
            ExtractionItemData material = LoadOrCreateAsset<ExtractionItemData>("PrototypeMaterial.asset");
            SetEnum(material, "category", (int)ExtractionItemCategory.Material);

            ExtractionItemData currency = LoadOrCreateAsset<ExtractionItemData>("PrototypeCurrency.asset");
            SetEnum(currency, "category", (int)ExtractionItemCategory.Currency);

            ExtractionItemData weaponItem = LoadOrCreateAsset<ExtractionItemData>("PrototypeWeaponItem.asset");
            SetEnum(weaponItem, "category", (int)ExtractionItemCategory.Weapon);

            ExtractionItemData mapUnlock = LoadOrCreateAsset<ExtractionItemData>("PrototypeMapUnlock.asset");
            SetEnum(mapUnlock, "category", (int)ExtractionItemCategory.MapUnlock);

            ExtractionWeaponSkillData skill = LoadOrCreateAsset<ExtractionWeaponSkillData>("PrototypeWeaponSkill.asset");
            SetFloat(skill, "cooldown", 4f);
            SetFloat(skill, "radius", 1.5f);
            SetFloat(skill, "damageMultiplier", 2f);
            SetInt(skill, "durabilityCost", 3);
            SetFloat(skill, "staminaCost", 24f);

            ExtractionWeaponData weapon = LoadOrCreateAsset<ExtractionWeaponData>("PrototypeWeapon.asset");
            SetObject(weapon, "item", weaponItem);
            SetFloat(weapon, "damage", 14f);
            SetFloat(weapon, "attackRange", 0.9f);
            SetFloat(weapon, "attackRadius", 0.55f);
            SetFloat(weapon, "attackCooldown", 0.45f);
            SetInt(weapon, "maxDurability", 40);
            SetInt(weapon, "durabilityLossPerAttack", 1);
            SetObject(weapon, "activeSkill", skill);

            ExtractionLootTableData enemyLoot = LoadOrCreateAsset<ExtractionLootTableData>("PrototypeEnemyLoot.asset");
            SetLootEntries(enemyLoot, material, 1, 3, 1f);

            ExtractionEnemyData enemy = LoadOrCreateAsset<ExtractionEnemyData>("PrototypeEnemy.asset");
            SetFloat(enemy, "maxHealth", 35f);
            SetFloat(enemy, "contactDamage", 8f);
            SetFloat(enemy, "chaseSpeed", 2.2f);
            SetFloat(enemy, "detectionRange", 6f);
            SetFloat(enemy, "attackRange", 0.75f);
            SetFloat(enemy, "attackCooldown", 1.2f);
            SetObject(enemy, "lootTable", enemyLoot);

            ExtractionLootTableData roomReward = LoadOrCreateAsset<ExtractionLootTableData>("PrototypeRoomReward.asset");
            SetLootEntries(roomReward, material, 2, 5, 1f);

            ExtractionRoomData startRoom = LoadOrCreateAsset<ExtractionRoomData>("PrototypeStartRoom.asset");
            ConfigureRoom(startRoom, ExtractionRoomType.Combat, 1, "Small supplies", "Low threat", roomReward, null);

            ExtractionRoomData supplyRoom = LoadOrCreateAsset<ExtractionRoomData>("PrototypeSupplyRoom.asset");
            ConfigureRoom(supplyRoom, ExtractionRoomType.Treasure, 1, "More supplies", "Few enemies", roomReward, null);

            ExtractionRoomData dangerRoom = LoadOrCreateAsset<ExtractionRoomData>("PrototypeDangerRoom.asset");
            ConfigureRoom(dangerRoom, ExtractionRoomType.Combat, 3, "Better drops", "Higher damage", roomReward, null);

            ExtractionRoomData extractRoom = LoadOrCreateAsset<ExtractionRoomData>("PrototypeExtractRoom.asset");
            ConfigureRoom(extractRoom, ExtractionRoomType.Extract, 0, "Extract safely", "Run ends", null, null);

            ExtractionRoomData bossRoom = LoadOrCreateAsset<ExtractionRoomData>("PrototypeBossRoom.asset");
            ConfigureRoom(bossRoom, ExtractionRoomType.Boss, 5, "Map unlock", "Boss placeholder", roomReward, mapUnlock);

            ExtractionMapData map = LoadOrCreateAsset<ExtractionMapData>("PrototypeMap.asset");
            SetObject(map, "startRoom", startRoom);
            SetObjectList(map, "roomPool", supplyRoom, dangerRoom, extractRoom);
            SetObject(map, "bossRoom", bossRoom);
            SetInt(map, "choicesPerRoom", 3);
            SetInt(map, "roomsBeforeBoss", 2);

            ExtractionBaseUpgradeData storageUpgrade = LoadOrCreateAsset<ExtractionBaseUpgradeData>("PrototypeStorageUpgrade.asset");
            SetUpgradeCost(storageUpgrade, material, 3);

            ExtractionRecipeData repairRecipe = LoadOrCreateAsset<ExtractionRecipeData>("PrototypeRepairRecipe.asset");
            SetRecipe(repairRecipe, material, 2, weaponItem, 1, 12);

            ExtractionRecipeData marketListing = LoadOrCreateAsset<ExtractionRecipeData>("PrototypeMarketListing.asset");
            SetRecipe(marketListing, currency, 1, material, 1, 0);

            AssetDatabase.SaveAssets();
            return new PrototypeContent(map, weapon, enemy);
        }

        private static void ConfigureRoom(
            ExtractionRoomData room,
            ExtractionRoomType roomType,
            int riskLevel,
            string rewardPreview,
            string riskPreview,
            ExtractionLootTableData rewardTable,
            ExtractionItemData mapUnlockItem)
        {
            SetEnum(room, "roomType", (int)roomType);
            SetInt(room, "riskLevel", riskLevel);
            SetString(room, "rewardPreview", rewardPreview);
            SetString(room, "riskPreview", riskPreview);
            SetObject(room, "roomRewardTable", rewardTable);
            SetObject(room, "mapUnlockItem", mapUnlockItem);
        }

        private static T LoadOrCreateAsset<T>(string fileName) where T : ScriptableObject
        {
            string path = GeneratedFolder + "/" + fileName;
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).enumValueIndex = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectList(Object target, string propertyName, params Object[] values)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty list = serialized.FindProperty(propertyName);
            list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLootEntries(ExtractionLootTableData table, ExtractionItemData item, int minQuantity, int maxQuantity, float chance)
        {
            SerializedObject serialized = new SerializedObject(table);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 1;
            SerializedProperty entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("minQuantity").intValue = minQuantity;
            entry.FindPropertyRelative("maxQuantity").intValue = maxQuantity;
            entry.FindPropertyRelative("dropChance").floatValue = chance;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetUpgradeCost(ExtractionBaseUpgradeData upgrade, ExtractionItemData item, int quantity)
        {
            SerializedObject serialized = new SerializedObject(upgrade);
            SerializedProperty cost = serialized.FindProperty("cost");
            cost.arraySize = 1;
            SerializedProperty entry = cost.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("item").objectReferenceValue = item;
            entry.FindPropertyRelative("quantity").intValue = quantity;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRecipe(
            ExtractionRecipeData recipe,
            ExtractionItemData inputItem,
            int inputQuantity,
            ExtractionItemData outputItem,
            int outputQuantity,
            int repairAmount)
        {
            SerializedObject serialized = new SerializedObject(recipe);
            SerializedProperty inputs = serialized.FindProperty("inputs");
            inputs.arraySize = inputItem != null ? 1 : 0;
            if (inputItem != null)
            {
                SerializedProperty input = inputs.GetArrayElementAtIndex(0);
                input.FindPropertyRelative("item").objectReferenceValue = inputItem;
                input.FindPropertyRelative("quantity").intValue = inputQuantity;
            }

            SerializedProperty outputs = serialized.FindProperty("outputs");
            outputs.arraySize = outputItem != null ? 1 : 0;
            if (outputItem != null)
            {
                SerializedProperty output = outputs.GetArrayElementAtIndex(0);
                output.FindPropertyRelative("item").objectReferenceValue = outputItem;
                output.FindPropertyRelative("quantity").intValue = outputQuantity;
            }

            serialized.FindProperty("weaponRepairAmount").intValue = repairAmount;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private readonly struct PrototypeContent
        {
            public PrototypeContent(ExtractionMapData mapData, ExtractionWeaponData weaponData, ExtractionEnemyData enemyData)
            {
                MapData = mapData;
                WeaponData = weaponData;
                EnemyData = enemyData;
            }

            public ExtractionMapData MapData { get; }
            public ExtractionWeaponData WeaponData { get; }
            public ExtractionEnemyData EnemyData { get; }
        }
    }

    [InitializeOnLoad]
    internal static class ExtractionPrototypeAutoBuilder
    {
        static ExtractionPrototypeAutoBuilder()
        {
            EditorApplication.delayCall += TryBuildMissingPrototypeScenes;
        }

        private static void TryBuildMissingPrototypeScenes()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            bool hasBase = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExtractionPrototypeSceneBuilder.BaseScenePath) != null;
            bool hasRun = AssetDatabase.LoadAssetAtPath<SceneAsset>(ExtractionPrototypeSceneBuilder.RunScenePath) != null;
            if (hasBase && hasRun)
            {
                return;
            }

            ExtractionPrototypeSceneBuilder.BuildPrototypeScene();
        }
    }
}
