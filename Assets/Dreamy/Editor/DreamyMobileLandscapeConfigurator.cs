using Dreamy;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dreamy.Editor
{
    public static class DreamyMobileLandscapeConfigurator
    {
        [MenuItem("Dreamy/Apply Mobile Landscape Layout", false, 2)]
        [MenuItem("Tools/Dreamy/Apply Mobile Landscape Layout", false, 2)]
        public static void ApplyLandscapeToOpenScene()
        {
            ApplyPlayerSettings();
            ApplySceneLayout();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Dreamy] Applied mobile landscape layout to the open scene.");
        }

        public static void ApplyPlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
        }

        private static void ApplySceneLayout()
        {
            GameObject systems = GameObject.Find("Game Systems") ?? new GameObject("Game Systems");
            if (systems.GetComponent<DreamyMobileOrientation>() == null)
            {
                systems.AddComponent<DreamyMobileOrientation>();
            }

            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 5.6f;

                DreamyCameraFollow follow = camera.GetComponent<DreamyCameraFollow>();
                if (follow != null)
                {
                    follow.Configure(5.6f, false, false, 0.16f, 115f);
                }
            }

            GameObject hud = GameObject.Find("Mobile HUD");
            if (hud == null)
            {
                return;
            }

            CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            ApplyResourceBarLayout(hud.transform);
            ApplyJoystickLayout(hud.transform);
        }

        private static void ApplyResourceBarLayout(Transform hud)
        {
            Transform resourceBar = hud.Find("Resource Bar");
            if (resourceBar == null)
            {
                return;
            }

            RectTransform barRect = resourceBar.GetComponent<RectTransform>();
            if (barRect != null)
            {
                barRect.anchorMin = new Vector2(0f, 1f);
                barRect.anchorMax = new Vector2(1f, 1f);
                barRect.pivot = new Vector2(0.5f, 0.5f);
                barRect.anchoredPosition = new Vector2(0f, -60f);
                barRect.sizeDelta = new Vector2(0f, 76f);
            }

            ApplyHudText(resourceBar, "Wood Text", 0.18f);
            ApplyHudText(resourceBar, "Gold Text", 0.50f);
            ApplyHudText(resourceBar, "Food Text", 0.82f);
        }

        private static void ApplyHudText(Transform parent, string name, float anchorX)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                return;
            }

            Text text = child.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = 32;
            }

            RectTransform rect = child.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(anchorX, 0.5f);
                rect.anchorMax = new Vector2(anchorX, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(320f, 60f);
            }
        }

        private static void ApplyJoystickLayout(Transform hud)
        {
            Transform joystick = hud.Find("Virtual Joystick");
            if (joystick == null)
            {
                return;
            }

            RectTransform rootRect = joystick.GetComponent<RectTransform>();
            UnityEngine.UI.Image rootImage = joystick.GetComponent<UnityEngine.UI.Image>();
            if (rootImage != null)
            {
                Color color = rootImage.color;
                color.a = 0f;
                rootImage.color = color;
                rootImage.raycastTarget = true;
            }

            if (rootRect != null)
            {
                rootRect.anchorMin = new Vector2(0f, 0f);
                rootRect.anchorMax = new Vector2(0f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = new Vector2(190f, 170f);
                rootRect.sizeDelta = new Vector2(252f, 252f);
            }

            RectTransform handleRect = null;
            Transform handle = joystick.Find("Joystick Handle");
            if (handle != null)
            {
                handleRect = handle.GetComponent<RectTransform>();
                UnityEngine.UI.Image handleImage = handle.GetComponent<UnityEngine.UI.Image>();
                if (handleImage != null)
                {
                    Color color = handleImage.color;
                    color.a = Mathf.Max(color.a, 0.94f);
                    handleImage.color = color;
                    handleImage.raycastTarget = false;
                    handleImage.preserveAspect = true;
                }

                if (handleRect != null)
                {
                    handleRect.anchorMin = new Vector2(0.5f, 0.5f);
                    handleRect.anchorMax = new Vector2(0.5f, 0.5f);
                    handleRect.pivot = new Vector2(0.5f, 0.5f);
                    handleRect.anchoredPosition = Vector2.zero;
                    handleRect.sizeDelta = new Vector2(104f, 104f);
                }
            }

            DreamyVirtualJoystick virtualJoystick = joystick.GetComponent<DreamyVirtualJoystick>();
            if (virtualJoystick != null && handleRect != null)
            {
                virtualJoystick.Bind(handleRect, 96f);
            }
        }
    }
}
