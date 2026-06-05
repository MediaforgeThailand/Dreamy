using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyGmTools : MonoBehaviour
    {
        private const int WindowId = 80421;
        private const float RefreshInterval = 0.5f;

        [SerializeField] private bool visible;
        [SerializeField] private Rect windowRect = new Rect(24f, 24f, 360f, 430f);

        private DreamyMobilePlayer player;
        private DreamyCameraFollow cameraFollow;
        private float nextRefreshTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForDebugRuns()
        {
            if (!CanUseGmTools() || Object.FindFirstObjectByType<DreamyGmTools>() != null)
            {
                return;
            }

            GameObject tools = new GameObject("GM Tools");
            Object.DontDestroyOnLoad(tools);
            tools.AddComponent<DreamyGmTools>();
        }

        private static bool CanUseGmTools()
        {
            return Application.isEditor || Debug.isDebugBuild;
        }

        private void Awake()
        {
            Object.DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!CanUseGmTools())
            {
                enabled = false;
                return;
            }

            if (Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyDown(KeyCode.F9))
            {
                visible = !visible;
            }

            if (Time.unscaledTime >= nextRefreshTime)
            {
                RefreshTargets();
            }
        }

        private void OnGUI()
        {
            if (!CanUseGmTools())
            {
                return;
            }

            if (!visible)
            {
                Rect buttonRect = new Rect(Screen.width - 96f, 16f, 80f, 44f);
                if (GUI.Button(buttonRect, "GM"))
                {
                    visible = true;
                }

                return;
            }

            windowRect = GUILayout.Window(WindowId, windowRect, DrawWindow, "GM Tools");
        }

        private void RefreshTargets()
        {
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            if (player == null)
            {
                player = Object.FindFirstObjectByType<DreamyMobilePlayer>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = Object.FindFirstObjectByType<DreamyCameraFollow>();
            }
        }

        private void DrawWindow(int id)
        {
            RefreshTargets();

            GUILayout.Label("Runtime tuning");
            GUILayout.Space(6f);

            DrawPlayerTools();
            GUILayout.Space(10f);
            DrawCameraTools();
            GUILayout.Space(10f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide", GUILayout.Height(34f)))
            {
                visible = false;
            }

            if (GUILayout.Button("Refresh", GUILayout.Height(34f)))
            {
                player = null;
                cameraFollow = null;
                RefreshTargets();
            }
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 28f));
        }

        private void DrawPlayerTools()
        {
            GUILayout.Label("Player");
            if (player == null)
            {
                GUILayout.Label("No DreamyMobilePlayer found.");
                return;
            }

            float speed = SliderField("Move Speed", player.MoveSpeed, 0f, 12f);
            if (!Mathf.Approximately(speed, player.MoveSpeed))
            {
                player.MoveSpeed = speed;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Walk 4.2"))
            {
                player.MoveSpeed = DreamyMobilePlayer.DefaultMoveSpeed;
            }

            if (GUILayout.Button("Run 7.0"))
            {
                player.MoveSpeed = 7f;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCameraTools()
        {
            GUILayout.Label("Camera");
            if (cameraFollow == null)
            {
                GUILayout.Label("No DreamyCameraFollow found.");
                return;
            }

            cameraFollow.FollowSpeedPercent = SliderField("Follow %", cameraFollow.FollowSpeedPercent, 40f, 200f);
            cameraFollow.SmoothTime = SliderField("Smooth Time", cameraFollow.SmoothTime, 0.02f, 0.6f);
            cameraFollow.EdgeSafeZonePercent = SliderField("Edge Safe %", cameraFollow.EdgeSafeZonePercent, 5f, 35f);
            cameraFollow.SafeZoneBoostMultiplier = SliderField("Edge Boost", cameraFollow.SafeZoneBoostMultiplier, 1f, 6f);
            cameraFollow.OrthographicSize = SliderField("Zoom Size", cameraFollow.OrthographicSize, 2.5f, 8f);
        }

        private static float SliderField(string label, float value, float min, float max)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(150f));
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(170f));
            GUILayout.EndHorizontal();
            return next;
        }
    }
}
