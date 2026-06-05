using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    [InitializeOnLoad]
    public static class DreamyEditorOneShotActions
    {
        private const string FirstSceneBuildRequestFileName = "DreamyFirstSceneBuild.request";
        private const string OpenBlockingToolRequestFileName = "DreamyOpenBlockingTool.request";
        private const string ApplyLandscapeRequestFileName = "DreamyApplyLandscape.request";
        private static double nextCheckTime;
        private static bool isRegistered;

        static DreamyEditorOneShotActions()
        {
            RegisterWatcher();
        }

        [InitializeOnLoadMethod]
        private static void RegisterWatcher()
        {
            if (isRegistered)
            {
                return;
            }

            isRegistered = true;
            EditorApplication.update += TryRunRequestedActions;
        }

        private static void TryRunRequestedActions()
        {
            if (EditorApplication.timeSinceStartup < nextCheckTime)
            {
                return;
            }

            nextCheckTime = EditorApplication.timeSinceStartup + 0.5d;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                return;
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string openBlockingToolPath = Path.Combine(projectRoot, "Temp", OpenBlockingToolRequestFileName);
            string applyLandscapePath = Path.Combine(projectRoot, "Temp", ApplyLandscapeRequestFileName);
            string requestPath = Path.Combine(projectRoot, "Temp", FirstSceneBuildRequestFileName);

            if (EditorApplication.isPlayingOrWillChangePlaymode && (File.Exists(openBlockingToolPath) || File.Exists(applyLandscapePath) || File.Exists(requestPath)))
            {
                return;
            }

            if (File.Exists(openBlockingToolPath))
            {
                File.Delete(openBlockingToolPath);
                Debug.Log("[Dreamy] One-shot request received: open level blocking tool.");
                DreamyLevelBlockingTool.OpenFloating();
            }

            if (File.Exists(applyLandscapePath))
            {
                Debug.Log("[Dreamy] One-shot request received: apply mobile landscape layout.");
                DreamyMobileLandscapeConfigurator.ApplyLandscapeToOpenScene();
                File.Delete(applyLandscapePath);
            }

            if (!File.Exists(requestPath))
            {
                return;
            }

            Debug.Log("[Dreamy] One-shot request received: build first scene map.");
            DreamyPrototypeSceneBuilder.BuildFirstSceneMap();
            File.Delete(requestPath);
        }
    }
}
