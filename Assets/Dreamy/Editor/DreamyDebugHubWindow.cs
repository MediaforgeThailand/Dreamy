using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Dreamy;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    public sealed class DreamyDebugHubWindow : EditorWindow
    {
        private const string AgentsPath = "AGENTS.md";
        private const string CodeContextPath = "Assets/Docs/CODEX_CONTEXT.md";
        private const string AssetContextPath = "Assets/Docs/context.md";
        private const string AuditReportPath = "Assets/Docs/DebugToolsAudit.md";
        private const string CombatProfilePath = "Assets/Resources/" + DreamyCombatTuningProfile.DefaultResourceName + ".asset";
        private const string VisualCatalogPath = "Assets/Resources/DreamyPrototypeVisualCatalog.asset";
        private const string MonsterCatalogPath = "Assets/Resources/DreamyMonsterCatalog.asset";
        private const string AxionControllerPath = "Assets/Dreamy/Generated/AxionAnimator/LittleAxionPrototype.controller";
        private const string AxionSpriteFolder = "Assets/Luneblade - Little Axion (Premium)/Sprite Sheet";
        private const string EnemyRootPath = "Assets/Tiny Swords (Enemy Pack)/Enemy Pack/Enemies";

        private static readonly string[] RequiredScenePaths =
        {
            "Assets/Dreamy/Scenes/DreamyMobilePrototype.unity",
            "Assets/Dreamy/Scenes/DreamyVillageMap.unity",
            "Assets/Dreamy/Scenes/Prototype/Prototype_Base.unity",
            "Assets/Dreamy/Scenes/Prototype/Prototype_Run.unity"
        };

        private static readonly string[] RequiredAxionSheets =
        {
            "Idle.png",
            "Run.png",
            "Dash.png",
            "Hurt.png",
            "Attack 3.png",
            "Super Smash.png"
        };

        private static readonly string[] DebugToolScripts =
        {
            "Assets/Dreamy/Editor/DreamyDebugHubWindow.cs",
            "Assets/Dreamy/Editor/DreamyCombatTuningWindow.cs",
            "Assets/Dreamy/Editor/DreamyLevelBlockingTool.cs",
            "Assets/Dreamy/Editor/DreamyAxionAnimatorBuilder.cs",
            "Assets/Dreamy/Editor/DreamyPrototypeVisualCatalogBuilder.cs",
            "Assets/Dreamy/Editor/DreamyMonsterCatalogBuilder.cs",
            "Assets/Dreamy/Scripts/DreamyTrainingDummy.cs",
            "Assets/Dreamy/Scripts/DreamyGmTools.cs",
            "Assets/Dreamy/Scripts/DreamyCharacterGrounding.cs"
        };

        private static readonly string[] RecommendedNextTools =
        {
            "Runtime overlay toggle for player stats, current combo step, active hitbox, target count, and cooldowns.",
            "Combat scenario runner that resets the dummy, places targets, and steps through the six-hit combo.",
            "VFX event library with named presets that can be attached to Combat Tuning events without code edits.",
            "Monster tuning window that mirrors player hit markers, ranges, damage, stagger, and movement speed.",
            "Mobile viewport checker for safe combat UI placement, touch button reach, and blocked-screen warnings.",
            "Economy and inventory debug tab for item grants, crafting inputs, storage, and extraction reward checks."
        };

        private readonly List<AuditItem> auditItems = new List<AuditItem>();
        private Vector2 scrollPosition;
        private string lastAuditSummary = "Audit has not run yet.";

        [MenuItem("Dreamy/Debug/Debug Hub", false, 18)]
        [MenuItem("Tools/Dreamy/Debug Hub", false, 18)]
        [MenuItem("Window/Dreamy/Debug Hub", false, 18)]
        public static void Open()
        {
            DreamyDebugHubWindow window = GetWindow<DreamyDebugHubWindow>("Dreamy Debug Hub");
            window.minSize = new Vector2(560f, 620f);
            window.Show();
        }

        private void OnEnable()
        {
            RunFullAudit();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Dreamy Debug Hub", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "One place for prototype debugging: open tuning tools, refresh generated assets, check missing setup, and save a report before deeper fixes.",
                MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawQuickActions();
            DrawRuntimeSelection();
            DrawAuditToolbar();
            DrawAuditResults();
            DrawRecommendedNextTools();
            EditorGUILayout.EndScrollView();
        }

        private void DrawQuickActions()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Combat Tuning", GUILayout.Height(30f)))
                {
                    DreamyCombatTuningWindow.Open();
                }

                if (GUILayout.Button("Level Blocking", GUILayout.Height(30f)))
                {
                    DreamyLevelBlockingTool.Open();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Axion Animator", GUILayout.Height(30f)))
                {
                    DreamyAxionAnimatorBuilder.EnsureAnimatorAssets();
                    RunFullAudit();
                }

                if (GUILayout.Button("Refresh Visual Catalog", GUILayout.Height(30f)))
                {
                    DreamyPrototypeVisualCatalogBuilder.EnsureCatalog();
                    RunFullAudit();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Monster Catalog", GUILayout.Height(30f)))
                {
                    DreamyMonsterCatalogBuilder.EnsureCatalog();
                    RunFullAudit();
                }

                if (GUILayout.Button("Apply Mobile Landscape", GUILayout.Height(30f)))
                {
                    DreamyMobileLandscapeConfigurator.ApplyLandscapeToOpenScene();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Combat Profile"))
                {
                    SelectAsset(CombatProfilePath);
                }

                if (GUILayout.Button("Select Axion Controller"))
                {
                    SelectAsset(AxionControllerPath);
                }

                if (GUILayout.Button("Select Visual Catalog"))
                {
                    SelectAsset(VisualCatalogPath);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open CODEX Context"))
                {
                    OpenProjectText(CodeContextPath);
                }

                if (GUILayout.Button("Open Asset Context"))
                {
                    OpenProjectText(AssetContextPath);
                }

                if (GUILayout.Button("Open Audit Report"))
                {
                    OpenProjectText(AuditReportPath);
                }
            }
        }

        private void DrawRuntimeSelection()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Play Mode Helpers", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Player"))
                {
                    SelectSceneObject(UnityEngine.Object.FindAnyObjectByType<DreamyPlayerCombat>());
                }

                if (GUILayout.Button("Select Dummy"))
                {
                    SelectSceneObject(UnityEngine.Object.FindAnyObjectByType<DreamyTrainingDummy>());
                }

                if (GUILayout.Button("Select Runtime"))
                {
                    SelectSceneObject(UnityEngine.Object.FindAnyObjectByType<DreamyScenePrototypeRuntime>());
                }
            }
        }

        private void DrawAuditToolbar()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Full Audit", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(lastAuditSummary, MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Full Audit", GUILayout.Height(30f)))
                {
                    RunFullAudit();
                }

                if (GUILayout.Button("Save Audit Report", GUILayout.Height(30f)))
                {
                    SaveAuditReport();
                }

                if (GUILayout.Button("Copy Report", GUILayout.Height(30f)))
                {
                    CopyReportToClipboard();
                }
            }
        }

        private void DrawAuditResults()
        {
            if (auditItems.Count == 0)
            {
                EditorGUILayout.HelpBox("No audit items yet. Press Run Full Audit.", MessageType.Warning);
                return;
            }

            for (int i = 0; i < auditItems.Count; i++)
            {
                AuditItem item = auditItems[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUIStyle badgeStyle = new GUIStyle(EditorStyles.boldLabel);
                        badgeStyle.normal.textColor = SeverityColor(item.Severity);
                        badgeStyle.fixedWidth = 56f;
                        EditorGUILayout.LabelField(SeverityLabel(item.Severity), badgeStyle);
                        EditorGUILayout.LabelField(item.Title, EditorStyles.boldLabel);
                    }

                    if (!string.IsNullOrWhiteSpace(item.Detail))
                    {
                        EditorGUILayout.LabelField(item.Detail, EditorStyles.wordWrappedMiniLabel);
                    }
                }
            }
        }

        private void DrawRecommendedNextTools()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Recommended Next Debug Tools", EditorStyles.boldLabel);
            for (int i = 0; i < RecommendedNextTools.Length; i++)
            {
                EditorGUILayout.LabelField("- " + RecommendedNextTools[i], EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void RunFullAudit()
        {
            auditItems.Clear();

            AddRootFileCheck("Working agreement", AgentsPath, true);
            AddAssetCheck("Core design context", CodeContextPath, true);
            AddAssetCheck("Asset/runtime context", AssetContextPath, true);

            for (int i = 0; i < RequiredScenePaths.Length; i++)
            {
                AddAssetCheck("Prototype scene", RequiredScenePaths[i], true);
            }

            AuditCombatTuningProfile();
            AuditGeneratedAssets();
            AuditAxionAssets();
            AuditDebugScripts();
            AuditRuntimeSceneState();

            int errors = 0;
            int warnings = 0;
            for (int i = 0; i < auditItems.Count; i++)
            {
                if (auditItems[i].Severity == AuditSeverity.Error)
                {
                    errors++;
                }
                else if (auditItems[i].Severity == AuditSeverity.Warning)
                {
                    warnings++;
                }
            }

            lastAuditSummary = "Last run: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                + " | OK " + (auditItems.Count - warnings - errors)
                + " | Warnings " + warnings
                + " | Errors " + errors;
            Repaint();
        }

        private void AuditCombatTuningProfile()
        {
            DreamyCombatTuningProfile profile = AssetDatabase.LoadAssetAtPath<DreamyCombatTuningProfile>(CombatProfilePath);
            if (profile == null)
            {
                Add(AuditSeverity.Error, "Combat tuning profile", "Missing " + CombatProfilePath + ". Combat Tuning cannot save shared frame/hitbox settings.");
                return;
            }

            profile.EnsureDefaults();
            Add(AuditSeverity.Ok, "Combat tuning profile", "Found shared profile used by runtime combat and generated Axion animation clips.");

            Add(profile.ComboStepCount >= 6 ? AuditSeverity.Ok : AuditSeverity.Warning,
                "Combo sequence",
                "Current combo has " + profile.ComboStepCount + " step(s). Target prototype flow expects at least 6: A1, A1, A2, A1, A2, A3.");

            for (int i = 0; i < DreamyCombatTuningProfile.NormalAttackCount; i++)
            {
                DreamyCombatActionTuning action = profile.GetNormalAttack(i);
                string actionName = string.IsNullOrWhiteSpace(action.DisplayName) ? "A" + (i + 1) : action.DisplayName;
                bool frameRangeOk = action.SourceFrameStart >= 0
                    && action.FrameCount > 0
                    && action.SourceFrameStart + action.FrameCount <= action.SourceFrameTotal;
                Add(frameRangeOk ? AuditSeverity.Ok : AuditSeverity.Error,
                    actionName + " frame range",
                    "Start " + action.SourceFrameStart + ", frames " + action.FrameCount + ", source total " + action.SourceFrameTotal + ".");

                bool hitboxOk = action.HitboxLength > 0f && action.HitboxWidth > 0f && action.HitMarkerNormalizedTime >= 0f && action.HitMarkerNormalizedTime <= 1f;
                Add(hitboxOk ? AuditSeverity.Ok : AuditSeverity.Warning,
                    actionName + " hitbox timing",
                    "Hit marker " + action.HitMarkerNormalizedTime.ToString("0.00")
                    + ", length " + action.HitboxLength.ToString("0.00")
                    + ", width " + action.HitboxWidth.ToString("0.00") + ".");

                if (i == 2)
                {
                    bool heavyAttackOk = action.FrameSpeedMultiplier <= 0.65f && action.SlowMultiplier <= 0.55f && action.SlowDuration > 0f;
                    Add(heavyAttackOk ? AuditSeverity.Ok : AuditSeverity.Warning,
                        "A3 heavy attack status",
                        "A3 speed " + action.FrameSpeedMultiplier.ToString("0.00")
                        + ", slow multiplier " + action.SlowMultiplier.ToString("0.00")
                        + ", slow duration " + action.SlowDuration.ToString("0.00") + ".");
                }
            }

            DreamyCombatActionTuning special = profile.SpecialAttack;
            bool specialOk = special != null && special.FrameCount > 0 && special.HitboxLength > 0f && special.HitboxWidth > 0f;
            Add(specialOk ? AuditSeverity.Ok : AuditSeverity.Warning,
                "Special skill tuning",
                specialOk ? "Super Smash has frame and hitbox data." : "Super Smash tuning is missing frame or hitbox values.");
        }

        private void AuditGeneratedAssets()
        {
            AddTypedAssetCheck<DreamyPrototypeVisualCatalog>("Runtime visual catalog", VisualCatalogPath, true);
            AddTypedAssetCheck<DreamyMonsterCatalog>("Monster catalog", MonsterCatalogPath, false);
            AddTypedAssetCheck<RuntimeAnimatorController>("Generated Axion animator", AxionControllerPath, true);
        }

        private void AuditAxionAssets()
        {
            AddFolderCheck("Little Axion sprite folder", AxionSpriteFolder, true);
            for (int i = 0; i < RequiredAxionSheets.Length; i++)
            {
                AddAssetCheck("Little Axion sheet", AxionSpriteFolder + "/" + RequiredAxionSheets[i], true);
            }

            AddFolderCheck("Enemy pack root", EnemyRootPath, false);
        }

        private void AuditDebugScripts()
        {
            for (int i = 0; i < DebugToolScripts.Length; i++)
            {
                AddAssetCheck("Debug tool script", DebugToolScripts[i], true);
            }
        }

        private void AuditRuntimeSceneState()
        {
            if (!EditorApplication.isPlaying)
            {
                Add(AuditSeverity.Warning, "Runtime selection helpers", "Select Player/Dummy/Runtime works after Play Mode starts.");
                return;
            }

            Add(UnityEngine.Object.FindAnyObjectByType<DreamyPlayerCombat>() != null ? AuditSeverity.Ok : AuditSeverity.Warning,
                "Runtime player combat",
                "Checks whether the active Play Mode scene has a DreamyPlayerCombat instance.");
            Add(UnityEngine.Object.FindAnyObjectByType<DreamyTrainingDummy>() != null ? AuditSeverity.Ok : AuditSeverity.Warning,
                "Training dummy",
                "Checks whether the active Play Mode scene has the infinite-health dummy.");
            Add(UnityEngine.Object.FindAnyObjectByType<DreamyScenePrototypeRuntime>() != null ? AuditSeverity.Ok : AuditSeverity.Warning,
                "Prototype runtime",
                "Checks whether DreamyScenePrototypeRuntime is active in Play Mode.");
        }

        private void AddRootFileCheck(string title, string path, bool required)
        {
            Add(File.Exists(path) ? AuditSeverity.Ok : required ? AuditSeverity.Error : AuditSeverity.Warning,
                title,
                File.Exists(path) ? "Found " + path + "." : "Missing " + path + ".");
        }

        private void AddAssetCheck(string title, string path, bool required)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null || File.Exists(path);
            Add(exists ? AuditSeverity.Ok : required ? AuditSeverity.Error : AuditSeverity.Warning,
                title,
                exists ? "Found " + path + "." : "Missing " + path + ".");
        }

        private void AddTypedAssetCheck<T>(string title, string path, bool required) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            Add(asset != null ? AuditSeverity.Ok : required ? AuditSeverity.Error : AuditSeverity.Warning,
                title,
                asset != null ? "Found " + path + "." : "Missing " + path + ".");
        }

        private void AddFolderCheck(string title, string path, bool required)
        {
            bool exists = AssetDatabase.IsValidFolder(path);
            Add(exists ? AuditSeverity.Ok : required ? AuditSeverity.Error : AuditSeverity.Warning,
                title,
                exists ? "Found " + path + "." : "Missing " + path + ".");
        }

        private void Add(AuditSeverity severity, string title, string detail)
        {
            auditItems.Add(new AuditItem(severity, title, detail));
        }

        private void SelectAsset(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                ShowNotification(new GUIContent("Missing asset: " + path));
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private void SelectSceneObject(Component component)
        {
            if (component == null)
            {
                ShowNotification(new GUIContent("No matching object in current scene."));
                return;
            }

            Selection.activeGameObject = component.gameObject;
            EditorGUIUtility.PingObject(component.gameObject);
        }

        private void OpenProjectText(string path)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
                return;
            }

            string fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                EditorUtility.OpenWithDefaultApp(fullPath);
            }
            else
            {
                ShowNotification(new GUIContent("Missing file: " + path));
            }
        }

        private void SaveAuditReport()
        {
            string report = BuildAuditReport();
            Directory.CreateDirectory(Path.GetDirectoryName(AuditReportPath) ?? "Assets/Docs");
            File.WriteAllText(AuditReportPath, report, Encoding.UTF8);
            AssetDatabase.ImportAsset(AuditReportPath);
            AssetDatabase.SaveAssets();
            ShowNotification(new GUIContent("Saved DebugToolsAudit.md"));
        }

        private void CopyReportToClipboard()
        {
            GUIUtility.systemCopyBuffer = BuildAuditReport();
            ShowNotification(new GUIContent("Audit copied."));
        }

        private string BuildAuditReport()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Dreamy Debug Tools Audit");
            builder.AppendLine();
            builder.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            builder.AppendLine();
            builder.AppendLine("## Summary");
            builder.AppendLine();
            builder.AppendLine(lastAuditSummary);
            builder.AppendLine();
            builder.AppendLine("## Results");
            builder.AppendLine();

            for (int i = 0; i < auditItems.Count; i++)
            {
                AuditItem item = auditItems[i];
                builder.AppendLine("- [" + SeverityLabel(item.Severity) + "] " + item.Title + " - " + item.Detail);
            }

            builder.AppendLine();
            builder.AppendLine("## Recommended Next Debug Tools");
            builder.AppendLine();
            for (int i = 0; i < RecommendedNextTools.Length; i++)
            {
                builder.AppendLine("- " + RecommendedNextTools[i]);
            }

            builder.AppendLine();
            builder.AppendLine("## How To Use");
            builder.AppendLine();
            builder.AppendLine("- Open `Dreamy > Debug > Debug Hub` first when tuning combat, animation, hitboxes, generated assets, or prototype scenes.");
            builder.AppendLine("- Use `Combat Tuning` for frame ranges, hit markers, hitboxes, damage/status, and optional VFX/action events.");
            builder.AppendLine("- Press `Save Audit Report` after changes so the next debugging pass starts with current evidence.");
            return builder.ToString();
        }

        private static string SeverityLabel(AuditSeverity severity)
        {
            return severity switch
            {
                AuditSeverity.Ok => "OK",
                AuditSeverity.Warning => "WARN",
                AuditSeverity.Error => "ERROR",
                _ => "INFO"
            };
        }

        private static Color SeverityColor(AuditSeverity severity)
        {
            return severity switch
            {
                AuditSeverity.Ok => new Color(0.28f, 0.72f, 0.38f),
                AuditSeverity.Warning => new Color(0.92f, 0.67f, 0.22f),
                AuditSeverity.Error => new Color(0.92f, 0.32f, 0.32f),
                _ => Color.white
            };
        }

        private readonly struct AuditItem
        {
            public readonly AuditSeverity Severity;
            public readonly string Title;
            public readonly string Detail;

            public AuditItem(AuditSeverity severity, string title, string detail)
            {
                Severity = severity;
                Title = title;
                Detail = detail;
            }
        }

        private enum AuditSeverity
        {
            Ok,
            Warning,
            Error
        }
    }
}
