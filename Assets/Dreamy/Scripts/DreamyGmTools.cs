using UnityEngine;

namespace Dreamy
{
    public sealed class DreamyGmTools : MonoBehaviour
    {
        private const int WindowId = 80421;
        private const float RefreshInterval = 0.5f;
        private const float WindowWidth = 300f;
        private const float WindowHeight = 430f;
        private const float ButtonHeight = 24f;
        private const float LabelWidth = 104f;
        private const float SliderWidth = 138f;

        [SerializeField] private bool visible;
        [SerializeField] private Rect windowRect = new Rect(24f, 24f, WindowWidth, WindowHeight);

        private DreamyMobilePlayer player;
        private DreamyCharacterStats characterStats;
        private DreamyInventory inventory;
        private DreamyExperience experience;
        private DreamyPlayerProgression progression;
        private DreamyCameraFollow cameraFollow;
        private DreamyCharacterGrounding characterGrounding;
        private Vector2 scrollPosition;
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

            KeepWindowOnScreen();
            windowRect = GUILayout.Window(
                WindowId,
                windowRect,
                DrawWindow,
                "GM",
                GUILayout.Width(windowRect.width),
                GUILayout.Height(windowRect.height));
        }

        private void RefreshTargets()
        {
            nextRefreshTime = Time.unscaledTime + RefreshInterval;
            if (player == null)
            {
                player = Object.FindFirstObjectByType<DreamyMobilePlayer>();
            }

            if (characterStats == null && player != null)
            {
                characterStats = player.CharacterStats;
            }

            if (inventory == null && player != null)
            {
                inventory = player.Inventory;
            }

            if (experience == null && player != null)
            {
                experience = player.Experience;
            }

            if (progression == null && player != null)
            {
                progression = player.GetComponent<DreamyPlayerProgression>();
            }

            if (characterGrounding == null && player != null)
            {
                characterGrounding = player.GetComponent<DreamyCharacterGrounding>();
            }

            if (characterStats == null)
            {
                characterStats = Object.FindFirstObjectByType<DreamyCharacterStats>();
            }

            if (inventory == null)
            {
                inventory = Object.FindFirstObjectByType<DreamyInventory>();
            }

            if (experience == null)
            {
                experience = Object.FindFirstObjectByType<DreamyExperience>();
            }

            if (progression == null)
            {
                progression = Object.FindFirstObjectByType<DreamyPlayerProgression>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = Object.FindFirstObjectByType<DreamyCameraFollow>();
            }

            if (characterGrounding == null)
            {
                characterGrounding = Object.FindFirstObjectByType<DreamyCharacterGrounding>();
            }
        }

        private void DrawWindow(int id)
        {
            RefreshTargets();

            GUILayout.Label("Runtime");
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(windowRect.height - 82f));

            DrawPlayerTools();
            GUILayout.Space(6f);
            DrawCharacterTools();
            GUILayout.Space(6f);
            DrawProgressionTools();
            GUILayout.Space(6f);
            DrawGroundingTools();
            GUILayout.Space(6f);
            DrawInventoryTools();
            GUILayout.Space(6f);
            DrawCameraTools();
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Hide", GUILayout.Height(ButtonHeight)))
            {
                visible = false;
            }

            if (GUILayout.Button("Refresh", GUILayout.Height(ButtonHeight)))
            {
                player = null;
                characterStats = null;
                inventory = null;
                experience = null;
                progression = null;
                cameraFollow = null;
                characterGrounding = null;
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
            if (GUILayout.Button("Walk", GUILayout.Height(ButtonHeight)))
            {
                player.MoveSpeed = DreamyMobilePlayer.DefaultMoveSpeed;
            }

            if (GUILayout.Button("Run", GUILayout.Height(ButtonHeight)))
            {
                player.MoveSpeed = 7f;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawCharacterTools()
        {
            GUILayout.Label("Character");
            if (characterStats == null)
            {
                GUILayout.Label("No DreamyCharacterStats found.");
                return;
            }

            characterStats.MaxHealth = SliderField("Max Health", characterStats.MaxHealth, 1f, 300f);
            characterStats.CurrentHealth = SliderField("Health", characterStats.CurrentHealth, 0f, characterStats.MaxHealth);
            characterStats.MaxStamina = SliderField("Max Stamina", characterStats.MaxStamina, 1f, 300f);
            characterStats.CurrentStamina = SliderField("Stamina", characterStats.CurrentStamina, 0f, characterStats.MaxStamina);
            characterStats.Damage = SliderField("Damage", characterStats.Damage, 0f, 100f);
            characterStats.Strength = SliderField("Str", characterStats.Strength, 0f, 80f);
            characterStats.Agility = SliderField("Agi", characterStats.Agility, 0f, 80f);
            characterStats.AttackSpeed = SliderField("Attack Speed", characterStats.AttackSpeed, 0.1f, 3f);
            characterStats.CriticalChance = SliderField("Crit Rate", characterStats.CriticalChance, 0f, 1f);
            characterStats.CriticalDamageMultiplier = SliderField("Crit Damage", characterStats.CriticalDamageMultiplier, 1f, 4f);
            characterStats.StatusResistance = SliderField("Status Resist", characterStats.StatusResistance, 0f, 1f);
            GUILayout.Label($"Effective ATK SPD x{characterStats.AttackSpeedMultiplier:0.00} | Move x{characterStats.MovementSpeedMultiplier:0.00}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-10 HP", GUILayout.Height(ButtonHeight)))
            {
                characterStats.TakeDamage(10f);
            }

            if (GUILayout.Button("Restore", GUILayout.Height(ButtonHeight)))
            {
                characterStats.CurrentHealth = characterStats.MaxHealth;
                characterStats.CurrentStamina = characterStats.MaxStamina;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Slow 50%", GUILayout.Height(ButtonHeight)))
            {
                characterStats.ApplySlow(0.5f, 2f);
            }

            if (GUILayout.Button("Stun", GUILayout.Height(ButtonHeight)))
            {
                characterStats.ApplyStun(1f);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawProgressionTools()
        {
            GUILayout.Label("Progression");
            if (experience == null)
            {
                GUILayout.Label("No DreamyExperience found.");
                return;
            }

            GUILayout.Label($"Level {experience.Level} | EXP {experience.CurrentExp}/{experience.ExpToNextLevel}");
            if (progression != null)
            {
                GUILayout.Label($"Coins {progression.Coins} | SP {progression.SkillPoints} | Unlock {progression.UnlockTokens}");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+10 EXP", GUILayout.Height(ButtonHeight)))
            {
                experience.AddExperience(10);
            }

            if (GUILayout.Button("+100 EXP", GUILayout.Height(ButtonHeight)))
            {
                experience.AddExperience(100);
            }
            GUILayout.EndHorizontal();

            if (progression == null)
            {
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+25 Coin", GUILayout.Height(ButtonHeight)))
            {
                progression.AddCoins(25);
            }

            if (GUILayout.Button("+1 SP", GUILayout.Height(ButtonHeight)))
            {
                progression.AddSkillPoints(1);
            }

            if (GUILayout.Button("+1 Unlock", GUILayout.Height(ButtonHeight)))
            {
                progression.AddUnlockTokens(1);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawInventoryTools()
        {
            GUILayout.Label("Inventory");
            if (inventory == null)
            {
                GUILayout.Label("No DreamyInventory found.");
                return;
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+Wood", GUILayout.Height(ButtonHeight)))
            {
                inventory.AddItem(DreamyItemId.Wood, 1);
            }

            if (GUILayout.Button("+Gold", GUILayout.Height(ButtonHeight)))
            {
                inventory.AddItem(DreamyItemId.Gold, 1);
            }

            if (GUILayout.Button("+Food", GUILayout.Height(ButtonHeight)))
            {
                inventory.AddItem(DreamyItemId.Food, 1);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < inventory.Items.Count; i++)
            {
                DreamyInventorySlot item = inventory.Items[i];
                GUILayout.Label($"{item.DisplayName}: {item.Quantity}");
            }
        }

        private void DrawGroundingTools()
        {
            GUILayout.Label("Shadow");
            if (characterGrounding == null)
            {
                GUILayout.Label("No DreamyCharacterGrounding found.");
                return;
            }

            characterGrounding.ShadowVisible = GUILayout.Toggle(characterGrounding.ShadowVisible, "Show Shadow");
            characterGrounding.ShadowXOffset = SliderField("Shadow X", characterGrounding.ShadowXOffset, -0.8f, 0.8f);
            characterGrounding.ShadowYOffset = SliderField("Shadow Y", characterGrounding.ShadowYOffset, -0.8f, 0.8f);
            characterGrounding.ShadowWidthScale = SliderField("Shadow W", characterGrounding.ShadowWidthScale, 0.1f, 1.2f);
            characterGrounding.ShadowHeight = SliderField("Shadow H", characterGrounding.ShadowHeight, 0.02f, 0.36f);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Foot", GUILayout.Height(ButtonHeight)))
            {
                characterGrounding.ShadowXOffset = 0f;
                characterGrounding.ShadowYOffset = 0f;
                characterGrounding.ShadowWidthScale = 0.36f;
                characterGrounding.ShadowHeight = 0.1f;
            }

            if (GUILayout.Button("Lower", GUILayout.Height(ButtonHeight)))
            {
                characterGrounding.ShadowXOffset = 0f;
                characterGrounding.ShadowYOffset = -0.18f;
                characterGrounding.ShadowWidthScale = 0.42f;
                characterGrounding.ShadowHeight = 0.1f;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Height(ButtonHeight)))
            {
                characterGrounding.SaveTuning();
            }

            if (GUILayout.Button("Load", GUILayout.Height(ButtonHeight)))
            {
                characterGrounding.LoadSavedTuning();
            }

            if (GUILayout.Button("Clear", GUILayout.Height(ButtonHeight)))
            {
                characterGrounding.ClearSavedTuning();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label(characterGrounding.HasSavedTuning() ? "Saved shadow tuning active." : "No saved shadow tuning.");
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
            GUILayout.Label($"{label}: {value:0.00}", GUILayout.Width(LabelWidth));
            float next = GUILayout.HorizontalSlider(value, min, max, GUILayout.Width(SliderWidth));
            GUILayout.EndHorizontal();
            return next;
        }

        private void KeepWindowOnScreen()
        {
            windowRect.width = Mathf.Min(WindowWidth, Mathf.Max(220f, Screen.width - 32f));
            windowRect.height = Mathf.Min(WindowHeight, Mathf.Max(260f, Screen.height - 32f));
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, Mathf.Max(0f, Screen.width - windowRect.width));
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, Mathf.Max(0f, Screen.height - windowRect.height));
        }
    }
}
