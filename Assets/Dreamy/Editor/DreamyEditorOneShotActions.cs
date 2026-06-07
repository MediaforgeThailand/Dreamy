using System.IO;
using UnityEditor;
using UnityEngine;

namespace Dreamy.Editor
{
    [InitializeOnLoad]
    public static class DreamyEditorOneShotActions
    {
        private const string FirstSceneBuildRequestFileName = "DreamyFirstSceneBuild.request";
        private const string Scene2BuildRequestFileName = "DreamyScene2Build.request";
        private const string VillageMapBuildRequestFileName = "DreamyVillageMapBuild.request";
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
            string scene2RequestPath = Path.Combine(projectRoot, "Temp", Scene2BuildRequestFileName);
            string villageMapRequestPath = Path.Combine(projectRoot, "Temp", VillageMapBuildRequestFileName);
            string requestPath = Path.Combine(projectRoot, "Temp", FirstSceneBuildRequestFileName);

            if (EditorApplication.isPlaying && (File.Exists(scene2RequestPath) || File.Exists(villageMapRequestPath)))
            {
                Debug.Log("[Dreamy] Map build request is waiting; exiting Play Mode first.");
                EditorApplication.isPlaying = false;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode
                && (File.Exists(openBlockingToolPath) || File.Exists(applyLandscapePath) || File.Exists(scene2RequestPath) || File.Exists(villageMapRequestPath) || File.Exists(requestPath)))
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

            if (File.Exists(scene2RequestPath))
            {
                Debug.Log("[Dreamy] One-shot request received: build scene 2 map.");
                DreamyPrototypeSceneBuilder.BuildScene2Map();
                File.Delete(scene2RequestPath);
            }

            if (File.Exists(villageMapRequestPath))
            {
                Debug.Log("[Dreamy] One-shot request received: build village map.");
                DreamyPrototypeSceneBuilder.BuildVillageMap();
                File.Delete(villageMapRequestPath);
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
