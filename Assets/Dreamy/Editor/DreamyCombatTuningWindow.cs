using Dreamy;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    public sealed class DreamyCombatTuningWindow : EditorWindow
    {
        private const string ProfilePath = "Assets/Resources/" + DreamyCombatTuningProfile.DefaultResourceName + ".asset";
        private static readonly string[] ActionTabs = { "A1", "A2", "A3", "SKL" };

        private DreamyCombatTuningProfile profile;
        private SerializedObject serializedProfile;
        private Vector2 scrollPosition;
        private int selectedActionIndex;
        private Vector2 previewDirection = Vector2.down;

        [MenuItem("Dreamy/Debug/Combat Tuning", false, 20)]
        [MenuItem("Tools/Dreamy/Combat Tuning", false, 20)]
        [MenuItem("Window/Dreamy/Combat Tuning", false, 20)]
        public static void Open()
        {
            DreamyCombatTuningWindow window = GetWindow<DreamyCombatTuningWindow>("Combat Tuning");
            window.minSize = new Vector2(420f, 540f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadProfile();
            SceneView.duringSceneGui += DrawScenePreview;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawScenePreview;
        }

        private void OnGUI()
        {
            if (profile == null)
            {
                DrawMissingProfile();
                return;
            }

            serializedProfile.Update();
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Combat Tuning", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Tune attack frames, hit timing, hitboxes, damage/status, and optional VFX events here. Press Save Profile so Play Mode uses the saved values.", MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            DrawToolbar();
            DrawGlobalSettings();
            DrawActionTabs();
            DrawSelectedAction();
            DrawScenePreviewControls();
            EditorGUILayout.EndScrollView();

            GUILayout.FlexibleSpace();
            DrawFooterButtons();

            if (serializedProfile.ApplyModifiedProperties())
            {
                profile.EnsureDefaults();
                EditorUtility.SetDirty(profile);
                SceneView.RepaintAll();
            }
        }

        private void DrawMissingProfile()
        {
            EditorGUILayout.HelpBox("No Combat Tuning Profile exists in Resources yet.", MessageType.Warning);
            if (GUILayout.Button("Create Default Combat Profile", GUILayout.Height(34f)))
            {
                profile = CreateDefaultProfileAsset();
                serializedProfile = new SerializedObject(profile);
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Select Profile"))
                {
                    Selection.activeObject = profile;
                    EditorGUIUtility.PingObject(profile);
                }

                if (GUILayout.Button("Find Player"))
                {
                    DreamyPlayerCombat combat = FindCombatPreviewTarget();
                    if (combat != null)
                    {
                        Selection.activeGameObject = combat.gameObject;
                        EditorGUIUtility.PingObject(combat.gameObject);
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Find Dummy"))
                {
                    DreamyTrainingDummy dummy = FindAnyObjectByType<DreamyTrainingDummy>();
                    if (dummy != null)
                    {
                        Selection.activeGameObject = dummy.gameObject;
                        EditorGUIUtility.PingObject(dummy.gameObject);
                    }
                }

                if (GUILayout.Button("Reset Profile Defaults"))
                {
                if (EditorUtility.DisplayDialog("Reset Combat Profile", "Reset every value in the Combat Tuning Profile back to defaults?", "Reset", "Cancel"))
                    {
                        Undo.RecordObject(profile, "Reset Combat Tuning Profile");
                        profile.ResetToDefaults();
                        EditorUtility.SetDirty(profile);
                        serializedProfile.Update();
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        private void DrawGlobalSettings()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Global", EditorStyles.boldLabel);
            DrawProperty("baseDamage", "Base Damage");
            DrawProperty("normalAttackMinimumDuration", "Normal Min Duration");
            DrawProperty("specialAttackMinimumDuration", "Special Min Duration");
            DrawProperty("normalStaminaCost", "Normal Stamina Cost");
            DrawProperty("specialStaminaCost", "Special Stamina Cost");
            DrawProperty("comboResetDelay", "Combo Reset Delay");
            DrawProperty("comboInputBufferWindow", "Input Buffer Window");
            DrawProperty("specialCooldown", "Special Cooldown");

            SerializedProperty comboSequence = serializedProfile.FindProperty("comboSequence");
            EditorGUILayout.PropertyField(comboSequence, new GUIContent("Combo Sequence"), true);
        }

        private void DrawActionTabs()
        {
            EditorGUILayout.Space(8f);
            selectedActionIndex = GUILayout.Toolbar(Mathf.Clamp(selectedActionIndex, 0, ActionTabs.Length - 1), ActionTabs);
        }

        private void DrawSelectedAction()
        {
            SerializedProperty action = GetSelectedActionProperty();
            if (action == null)
            {
                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(GetActionTitle(action), EditorStyles.boldLabel);
            EditorGUILayout.Space(3f);

            EditorGUILayout.LabelField("Animation Frames", EditorStyles.miniBoldLabel);
            DrawRelative(action, "displayName", "Name");
            DrawRelative(action, "sourceFrameTotal", "Source Frame Total");
            DrawRelative(action, "sourceFrameStart", "Start Frame");
            DrawRelative(action, "frameCount", "Frame Count");
            DrawRelative(action, "animatorStateIndex", "Animator State");
            DrawRelative(action, "attackPartIndex", "Attack Part");
            DrawRelative(action, "frameSpeedMultiplier", "Frame Speed");

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Hit Timing And Box", EditorStyles.miniBoldLabel);
            SerializedProperty hitMarker = action.FindPropertyRelative("hitMarkerNormalizedTime");
            hitMarker.floatValue = EditorGUILayout.Slider("Hit Marker", hitMarker.floatValue, 0f, 1f);
            int frameCount = Mathf.Max(1, action.FindPropertyRelative("frameCount").intValue);
            int hitFrame = Mathf.Clamp(Mathf.RoundToInt(hitMarker.floatValue * frameCount), 0, frameCount - 1);
            EditorGUILayout.HelpBox("Hit marker is approximately frame " + hitFrame + " of " + frameCount + " for this action.", MessageType.None);
            DrawRelative(action, "originDistance", "Origin Distance");
            DrawRelative(action, "hitboxLength", "Hitbox Length");
            DrawRelative(action, "hitboxWidth", "Hitbox Width");

            EditorGUILayout.Space(5f);
            EditorGUILayout.LabelField("Damage And Status", EditorStyles.miniBoldLabel);
            DrawRelative(action, "damageMultiplier", "Damage Multiplier");
            DrawRelative(action, "knockbackForce", "Knockback");
            DrawRelative(action, "slowMultiplier", "Slow Multiplier");
            DrawRelative(action, "slowDuration", "Slow Duration");
            DrawRelative(action, "stunDuration", "Stun Duration");

            EditorGUILayout.Space(5f);
            DrawVfxEvents(action);
        }

        private void DrawVfxEvents(SerializedProperty action)
        {
            SerializedProperty events = action.FindPropertyRelative("events");
            EditorGUILayout.LabelField("VFX / Action Events", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(events, true);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Built-In Slash At Hit"))
                {
                    AddBuiltInSlashEvent(action, events);
                }

                if (GUILayout.Button("Clear Events"))
                {
                    events.ClearArray();
                }
            }
        }

        private void DrawScenePreviewControls()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Scene Preview Direction", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Toggle(previewDirection == Vector2.left, "Left", EditorStyles.miniButtonLeft))
                {
                    previewDirection = Vector2.left;
                }

                if (GUILayout.Toggle(previewDirection == Vector2.down, "Down", EditorStyles.miniButtonMid))
                {
                    previewDirection = Vector2.down;
                }

                if (GUILayout.Toggle(previewDirection == Vector2.right, "Right", EditorStyles.miniButtonMid))
                {
                    previewDirection = Vector2.right;
                }

                if (GUILayout.Toggle(previewDirection == Vector2.up, "Up", EditorStyles.miniButtonRight))
                {
                    previewDirection = Vector2.up;
                }
            }
        }

        private void DrawFooterButtons()
        {
            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply To Live Player", GUILayout.Height(34f)))
                {
                    SaveProfile(false);
                    ApplyToLivePlayers();
                }

                if (GUILayout.Button("Regenerate Animator", GUILayout.Height(34f)))
                {
                    SaveProfile(false);
                    DreamyAxionAnimatorBuilder.EnsureAnimatorAssets();
                }

                if (GUILayout.Button("Save Profile", GUILayout.Height(34f)))
                {
                    SaveProfile(true);
                }
            }
        }

        private void AddBuiltInSlashEvent(SerializedProperty action, SerializedProperty events)
        {
            events.arraySize++;
            SerializedProperty item = events.GetArrayElementAtIndex(events.arraySize - 1);
            item.FindPropertyRelative("enabled").boolValue = true;
            item.FindPropertyRelative("label").stringValue = GetActionTitle(action) + " Slash";
            item.FindPropertyRelative("normalizedTime").floatValue = action.FindPropertyRelative("hitMarkerNormalizedTime").floatValue;
            item.FindPropertyRelative("vfxKind").enumValueIndex = (int)DreamyCombatVfxKind.BuiltInSlash;
            item.FindPropertyRelative("offset").vector2Value = new Vector2(action.FindPropertyRelative("originDistance").floatValue + 0.42f, 0f);
            item.FindPropertyRelative("color").colorValue = new Color(1f, 0.86f, 0.36f, 0.9f);
            item.FindPropertyRelative("duration").floatValue = 0.15f;
            item.FindPropertyRelative("startScale").floatValue = 0.72f;
            item.FindPropertyRelative("endScale").floatValue = 1.22f;
            item.FindPropertyRelative("rotateToDirection").boolValue = true;
        }

        private void SaveProfile(bool showDialog)
        {
            serializedProfile.ApplyModifiedProperties();
            profile.EnsureDefaults();
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            if (showDialog)
            {
                ShowNotification(new GUIContent("Combat profile saved"));
            }
        }

        private void ApplyToLivePlayers()
        {
            DreamyPlayerCombat[] combats = FindObjectsByType<DreamyPlayerCombat>(FindObjectsInactive.Exclude);
            for (int i = 0; i < combats.Length; i++)
            {
                combats[i].ApplyTuningProfile(profile);
                EditorUtility.SetDirty(combats[i]);
            }

            ShowNotification(new GUIContent("Applied to " + combats.Length + " player combat component(s)"));
            SceneView.RepaintAll();
        }

        private void DrawScenePreview(SceneView sceneView)
        {
            if (profile == null || selectedActionIndex < 0)
            {
                return;
            }

            profile.EnsureDefaults();
            DreamyCombatActionTuning action = selectedActionIndex >= DreamyCombatTuningProfile.NormalAttackCount
                ? profile.SpecialAttack
                : profile.GetNormalAttack(selectedActionIndex);
            if (action == null)
            {
                return;
            }

            Vector3 center = ResolvePreviewOrigin();
            Vector2 direction = previewDirection.sqrMagnitude >= 0.01f ? previewDirection.normalized : Vector2.down;
            DrawHitboxHandles(center, direction, action);
            DrawEventHandles(center, direction, action);
        }

        private void DrawHitboxHandles(Vector3 center, Vector2 direction, DreamyCombatActionTuning action)
        {
            Vector2 forward = direction.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x) * (action.HitboxWidth * 0.5f);
            Vector3 origin = center + (Vector3)(forward * action.OriginDistance);
            Vector3 front = origin + (Vector3)(forward * action.HitboxLength);
            Vector3[] corners =
            {
                origin + (Vector3)side,
                front + (Vector3)side,
                front - (Vector3)side,
                origin - (Vector3)side
            };

            Color fill = selectedActionIndex == 0
                ? new Color(1f, 0.82f, 0.18f, 0.12f)
                : selectedActionIndex == 1
                    ? new Color(1f, 0.62f, 0.18f, 0.13f)
                    : selectedActionIndex == 2
                        ? new Color(1f, 0.28f, 0.12f, 0.14f)
                        : new Color(0.25f, 0.92f, 1f, 0.12f);
            Color outline = new Color(fill.r, fill.g, fill.b, 0.9f);
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
            Handles.color = Color.white;
            Handles.DrawLine(center, origin);
            Handles.Label(front, action.DisplayName + "\nHit " + Mathf.RoundToInt(action.HitMarkerNormalizedTime * 100f) + "%");
        }

        private void DrawEventHandles(Vector3 center, Vector2 direction, DreamyCombatActionTuning action)
        {
            DreamyCombatEventTuning[] events = action.Events;
            Vector2 forward = direction.normalized;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Handles.color = new Color(0.58f, 0.93f, 1f, 0.95f);
            for (int i = 0; i < events.Length; i++)
            {
                DreamyCombatEventTuning combatEvent = events[i];
                if (combatEvent == null || !combatEvent.Enabled || combatEvent.VfxKind == DreamyCombatVfxKind.None)
                {
                    continue;
                }

                Vector2 offset = combatEvent.Offset;
                Vector3 position = center + (Vector3)(forward * offset.x + side * offset.y);
                Handles.DrawWireDisc(position, Vector3.forward, 0.08f);
                Handles.Label(position + Vector3.up * 0.12f, combatEvent.Label + " " + Mathf.RoundToInt(combatEvent.NormalizedTime * 100f) + "%");
            }
        }

        private Vector3 ResolvePreviewOrigin()
        {
            DreamyPlayerCombat combat = FindCombatPreviewTarget();
            return combat != null ? combat.transform.position : Vector3.zero;
        }

        private DreamyPlayerCombat FindCombatPreviewTarget()
        {
            if (Selection.activeGameObject != null)
            {
                DreamyPlayerCombat selected = Selection.activeGameObject.GetComponentInParent<DreamyPlayerCombat>();
                if (selected != null)
                {
                    return selected;
                }
            }

            return FindAnyObjectByType<DreamyPlayerCombat>();
        }

        private SerializedProperty GetSelectedActionProperty()
        {
            if (selectedActionIndex >= DreamyCombatTuningProfile.NormalAttackCount)
            {
                return serializedProfile.FindProperty("specialAttack");
            }

            SerializedProperty attacks = serializedProfile.FindProperty("normalAttacks");
            if (attacks == null || selectedActionIndex >= attacks.arraySize)
            {
                return null;
            }

            return attacks.GetArrayElementAtIndex(selectedActionIndex);
        }

        private static string GetActionTitle(SerializedProperty action)
        {
            if (action == null)
            {
                return "Action";
            }

            SerializedProperty displayName = action.FindPropertyRelative("displayName");
            return displayName != null && !string.IsNullOrWhiteSpace(displayName.stringValue) ? displayName.stringValue : "Action";
        }

        private void DrawProperty(string propertyName, string label)
        {
            SerializedProperty property = serializedProfile.FindProperty(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private static void DrawRelative(SerializedProperty parent, string propertyName, string label)
        {
            SerializedProperty property = parent.FindPropertyRelative(propertyName);
            if (property != null)
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label));
            }
        }

        private void LoadProfile()
        {
            profile = AssetDatabase.LoadAssetAtPath<DreamyCombatTuningProfile>(ProfilePath);
            if (profile == null)
            {
                profile = CreateDefaultProfileAsset();
            }

            serializedProfile = profile != null ? new SerializedObject(profile) : null;
        }

        private static DreamyCombatTuningProfile CreateDefaultProfileAsset()
        {
            EnsureResourcesFolder();
            DreamyCombatTuningProfile created = CreateInstance<DreamyCombatTuningProfile>();
            created.ResetToDefaults();
            AssetDatabase.CreateAsset(created, ProfilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<DreamyCombatTuningProfile>(ProfilePath);
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
        }
    }
}
