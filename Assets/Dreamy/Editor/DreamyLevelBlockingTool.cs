using Dreamy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dreamy.Editor
{
    public sealed class DreamyLevelBlockingTool : EditorWindow
    {
        private const string LevelRootPath = "World/Level Root";
        private const string ManualBlockerRootName = "Manual Level Blockers";

        private bool drawMode;
        private bool snapToGrid = true;
        private float gridSize = 0.48f;
        private DreamyLevelBlockerKind blockerKind = DreamyLevelBlockerKind.Blocker;
        private Color blockerColor = new Color(0.06f, 0.08f, 0.10f, 0.32f);
        private Vector2 dragStart;
        private Vector2 dragCurrent;
        private bool isDragging;

        [MenuItem("Dreamy/Level Blocking Tool", false, 10)]
        [MenuItem("Tools/Dreamy/Level Blocking Tool", false, 10)]
        [MenuItem("Window/Dreamy/Level Blocking Tool", false, 10)]
        public static void Open()
        {
            DreamyLevelBlockingTool window = GetWindow<DreamyLevelBlockingTool>("Level Blocking");
            window.minSize = new Vector2(320f, 420f);
            window.Show();
            window.Focus();
        }

        [MenuItem("Dreamy/Level Blocking Tool Floating", false, 11)]
        [MenuItem("Tools/Dreamy/Level Blocking Tool Floating", false, 11)]
        public static void OpenFloating()
        {
            DreamyLevelBlockingTool window = CreateInstance<DreamyLevelBlockingTool>();
            window.titleContent = new GUIContent("Level Blocking");
            window.minSize = new Vector2(320f, 420f);
            window.ShowUtility();
            window.Focus();
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DuringSceneGui;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGui;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Manual Level Blocking", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Turn on Draw Blockers, then drag in the Scene view to create non-walkable BoxCollider2D areas.", MessageType.Info);

            drawMode = EditorGUILayout.ToggleLeft("Draw Blockers In Scene View", drawMode, EditorStyles.boldLabel);
            blockerKind = (DreamyLevelBlockerKind)EditorGUILayout.EnumPopup("Kind", blockerKind);
            blockerColor = EditorGUILayout.ColorField("Overlay Color", blockerColor);
            snapToGrid = EditorGUILayout.Toggle("Snap To Grid", snapToGrid);
            using (new EditorGUI.DisabledScope(!snapToGrid))
            {
                gridSize = Mathf.Max(0.05f, EditorGUILayout.FloatField("Grid Size", gridSize));
            }

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Create Root"))
                {
                    Selection.activeGameObject = GetOrCreateBlockerRoot();
                }

                if (GUILayout.Button("Select Root"))
                {
                    Selection.activeGameObject = GetOrCreateBlockerRoot();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Delete Selected Blockers"))
                {
                    DeleteSelectedBlockers();
                }

                if (GUILayout.Button("Save Scene"))
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                    EditorSceneManager.SaveOpenScenes();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Tips", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Use the Scene view, not the Game view.");
            EditorGUILayout.LabelField("- Hold Alt to orbit/pan without drawing.");
            EditorGUILayout.LabelField("- Select a blocker to resize it with Unity's Rect/Scale tools.");
        }

        private void DuringSceneGui(SceneView sceneView)
        {
            DrawExistingBlockers();

            if (!drawMode)
            {
                return;
            }

            Event current = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            if (current.alt || current.button != 0)
            {
                return;
            }

            HandleUtility.AddDefaultControl(controlId);

            if (current.type == EventType.MouseDown)
            {
                dragStart = GetMouseWorldPosition(current.mousePosition);
                dragCurrent = dragStart;
                isDragging = true;
                current.Use();
            }
            else if (current.type == EventType.MouseDrag && isDragging)
            {
                dragCurrent = GetMouseWorldPosition(current.mousePosition);
                sceneView.Repaint();
                current.Use();
            }
            else if (current.type == EventType.MouseUp && isDragging)
            {
                dragCurrent = GetMouseWorldPosition(current.mousePosition);
                CreateBlockerFromDrag();
                isDragging = false;
                current.Use();
            }

            if (isDragging)
            {
                DrawPreviewRect(dragStart, dragCurrent);
            }
        }

        private Vector2 GetMouseWorldPosition(Vector2 mousePosition)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            if (!plane.Raycast(ray, out float distance))
            {
                return Vector2.zero;
            }

            Vector3 world = ray.GetPoint(distance);
            Vector2 position = new Vector2(world.x, world.y);
            return snapToGrid ? Snap(position) : position;
        }

        private Vector2 Snap(Vector2 position)
        {
            float size = Mathf.Max(0.05f, gridSize);
            return new Vector2(
                Mathf.Round(position.x / size) * size,
                Mathf.Round(position.y / size) * size);
        }

        private void CreateBlockerFromDrag()
        {
            Vector2 min = Vector2.Min(dragStart, dragCurrent);
            Vector2 max = Vector2.Max(dragStart, dragCurrent);
            Vector2 size = max - min;
            if (size.x < 0.08f || size.y < 0.08f)
            {
                return;
            }

            GameObject root = GetOrCreateBlockerRoot();
            GameObject blocker = new GameObject($"Manual Blocker {root.transform.childCount + 1:00}");
            Undo.RegisterCreatedObjectUndo(blocker, "Create Level Blocker");
            blocker.transform.SetParent(root.transform, false);
            blocker.transform.position = new Vector3((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f, 0f);

            BoxCollider2D box = blocker.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = false;

            DreamyLevelBlocker marker = blocker.AddComponent<DreamyLevelBlocker>();
            marker.Configure(blockerKind, blockerColor);

            Selection.activeGameObject = blocker;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        private GameObject GetOrCreateBlockerRoot()
        {
            GameObject levelRoot = GameObject.Find(LevelRootPath);
            if (levelRoot == null)
            {
                GameObject world = GameObject.Find("World") ?? new GameObject("World");
                levelRoot = new GameObject("Level Root");
                levelRoot.transform.SetParent(world.transform, false);
                Undo.RegisterCreatedObjectUndo(levelRoot, "Create Level Root");
            }

            Transform existing = levelRoot.transform.Find(ManualBlockerRootName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject root = new GameObject(ManualBlockerRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Manual Blocker Root");
            root.transform.SetParent(levelRoot.transform, false);
            root.isStatic = true;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            return root;
        }

        private void DrawPreviewRect(Vector2 start, Vector2 end)
        {
            Vector2 min = Vector2.Min(start, end);
            Vector2 max = Vector2.Max(start, end);
            DrawSceneRect(min, max, blockerColor, 0.55f);
        }

        private void DrawExistingBlockers()
        {
            DreamyLevelBlocker[] blockers = Object.FindObjectsByType<DreamyLevelBlocker>(FindObjectsInactive.Include);
            for (int i = 0; i < blockers.Length; i++)
            {
                BoxCollider2D box = blockers[i].GetComponent<BoxCollider2D>();
                if (box == null)
                {
                    continue;
                }

                Bounds bounds = box.bounds;
                Vector2 min = new Vector2(bounds.min.x, bounds.min.y);
                Vector2 max = new Vector2(bounds.max.x, bounds.max.y);
                DrawSceneRect(min, max, blockerColor, 0.22f);
            }
        }

        private static void DrawSceneRect(Vector2 min, Vector2 max, Color color, float alpha)
        {
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, 0f),
                new Vector3(min.x, max.y, 0f),
                new Vector3(max.x, max.y, 0f),
                new Vector3(max.x, min.y, 0f)
            };

            Color fill = new Color(color.r, color.g, color.b, alpha);
            Color outline = new Color(color.r, color.g, color.b, 0.95f);
            Handles.DrawSolidRectangleWithOutline(corners, fill, outline);
        }

        private static void DeleteSelectedBlockers()
        {
            GameObject[] selected = Selection.gameObjects;
            for (int i = 0; i < selected.Length; i++)
            {
                if (selected[i].GetComponent<DreamyLevelBlocker>() == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(selected[i]);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
