using System.IO;
using Dreamy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Dreamy.Editor
{
    [InitializeOnLoad]
    internal static class DreamyAxionAnimatorBuilder
    {
        private const string SpriteFolder = "Assets/Luneblade - Little Axion (Premium)/Sprite Sheet";
        private const string GeneratedFolder = "Assets/Dreamy/Generated";
        private const string AnimatorFolder = GeneratedFolder + "/AxionAnimator";
        private const string ControllerPath = AnimatorFolder + "/LittleAxionPrototype.controller";
        private const int CellSize = 144;
        private const float PixelsPerUnit = 48f;
        private const string IsMovingParameter = "IsMoving";
        private const string Attack1Parameter = "Attack1";
        private const string Attack2Parameter = "Attack2";
        private const string Attack3Parameter = "Attack3";
        private const string SuperSmashParameter = "SuperSmash";
        private const string DashParameter = "Dash";
        private const string HurtParameter = "Hurt";
        private const string HitEventName = "DreamyAnimationHitMarker";

        private static readonly AxionAnimationDefinition[] Animations =
        {
            new AxionAnimationDefinition("Idle", "Idle.png", 7, 5f, true, -1f),
            new AxionAnimationDefinition("Run", "Run.png", 8, 13f, true, -1f),
            new AxionAnimationDefinition("Dash", "Dash.png", 12, 24f, false, -1f),
            new AxionAnimationDefinition("Hurt", "Hurt.png", 3, 12f, false, -1f),
            new AxionAnimationDefinition("Attack 1", "Attack 3.png", 9, 24f, false, 0.45f, 0, 23),
            new AxionAnimationDefinition("Attack 2", "Attack 3.png", 6, 24f, false, 0.22f, 9, 23),
            new AxionAnimationDefinition("Attack 3", "Attack 3.png", 8, 24f, false, 0.18f, 15, 23),
            new AxionAnimationDefinition("Super Smash", "Super Smash.png", 15, 18f, false, 0.52f)
        };

        static DreamyAxionAnimatorBuilder()
        {
            EditorApplication.delayCall += EnsureAnimatorAssets;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        [MenuItem("Dreamy/Prototype/Refresh Little Axion Animator")]
        public static void EnsureAnimatorAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || !AssetDatabase.IsValidFolder(SpriteFolder))
            {
                return;
            }

            EnsureFolder(GeneratedFolder);
            EnsureFolder(AnimatorFolder);

            for (int i = 0; i < Animations.Length; i++)
            {
                string sheetPath = SheetPath(Animations[i].FileName);
                ConfigureMultipleSpriteSheet(sheetPath, Animations[i].SourceFrameCount);
            }

            AssetDatabase.Refresh();

            for (int i = 0; i < Animations.Length; i++)
            {
                BuildClip(Animations[i]);
            }

            BuildController();
            AssetDatabase.SaveAssets();
            DreamyPrototypeVisualCatalogBuilder.EnsureCatalog();
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.delayCall += EnsureAnimatorAssets;
            }
        }

        private static void ConfigureMultipleSpriteSheet(string path, int frameCount)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return;
            }

            bool changed = false;
            changed |= SetImporterValue(importer.textureType != TextureImporterType.Sprite, () => importer.textureType = TextureImporterType.Sprite);
            changed |= SetImporterValue(importer.spriteImportMode != SpriteImportMode.Multiple, () => importer.spriteImportMode = SpriteImportMode.Multiple);
            changed |= SetImporterValue(!Mathf.Approximately(importer.spritePixelsPerUnit, PixelsPerUnit), () => importer.spritePixelsPerUnit = PixelsPerUnit);
            changed |= SetImporterValue(importer.filterMode != FilterMode.Point, () => importer.filterMode = FilterMode.Point);
            changed |= SetImporterValue(importer.mipmapEnabled, () => importer.mipmapEnabled = false);
            changed |= SetImporterValue(importer.textureCompression != TextureImporterCompression.Uncompressed, () => importer.textureCompression = TextureImporterCompression.Uncompressed);
            changed |= SetImporterValue(importer.npotScale != TextureImporterNPOTScale.None, () => importer.npotScale = TextureImporterNPOTScale.None);
            changed |= SetImporterValue(!importer.alphaIsTransparency, () => importer.alphaIsTransparency = true);
            changed |= SetImporterValue(importer.wrapMode != TextureWrapMode.Clamp, () => importer.wrapMode = TextureWrapMode.Clamp);
            changed |= SetImporterValue(importer.anisoLevel != 0, () => importer.anisoLevel = 0);

            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            changed |= SetPlatform(importer, standalone, "Standalone");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            changed |= SetPlatform(importer, android, "Android");
            TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings("iPhone");
            changed |= SetPlatform(importer, ios, "iPhone");

            SpriteMetaData[] sprites = BuildSpriteMetaData(path, frameCount);
            if (importer.spritesheet == null || importer.spritesheet.Length != sprites.Length)
            {
                importer.spritesheet = sprites;
                changed = true;
            }
            else
            {
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (importer.spritesheet[i].name != sprites[i].name || importer.spritesheet[i].rect != sprites[i].rect)
                    {
                        importer.spritesheet = sprites;
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static SpriteMetaData[] BuildSpriteMetaData(string path, int frameCount)
        {
            string prefix = Path.GetFileNameWithoutExtension(path).Replace(" ", string.Empty);
            SpriteMetaData[] sprites = new SpriteMetaData[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                sprites[i] = new SpriteMetaData
                {
                    name = prefix + "_" + i.ToString("00"),
                    rect = new Rect(i * CellSize, 0f, CellSize, CellSize),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.28f)
                };
            }

            return sprites;
        }

        private static void BuildClip(AxionAnimationDefinition animation)
        {
            Sprite[] sprites = LoadSprites(
                SheetPath(animation.FileName),
                animation.SourceFrameCount,
                animation.FirstFrameIndex,
                animation.FrameCount);
            if (sprites.Length == 0)
            {
                return;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(animation.StateName));
            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, ClipPath(animation.StateName));
            }

            clip.name = "LittleAxion_" + animation.StateName.Replace(" ", string.Empty);
            clip.frameRate = animation.FramesPerSecond;

            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };

            ObjectReferenceKeyframe[] keyframes = new ObjectReferenceKeyframe[sprites.Length + 1];
            for (int i = 0; i < sprites.Length; i++)
            {
                keyframes[i] = new ObjectReferenceKeyframe
                {
                    time = i / animation.FramesPerSecond,
                    value = sprites[i]
                };
            }

            keyframes[sprites.Length] = new ObjectReferenceKeyframe
            {
                time = sprites.Length / animation.FramesPerSecond,
                value = sprites[sprites.Length - 1]
            };

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = animation.Loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (animation.HitEventNormalizedTime >= 0f)
            {
                AnimationEvent hitEvent = new AnimationEvent
                {
                    functionName = HitEventName,
                    time = Mathf.Clamp(clip.length * animation.HitEventNormalizedTime, 0f, Mathf.Max(0f, clip.length - 0.01f))
                };
                AnimationUtility.SetAnimationEvents(clip, new[] { hitEvent });
            }
            else
            {
                AnimationUtility.SetAnimationEvents(clip, System.Array.Empty<AnimationEvent>());
            }

            EditorUtility.SetDirty(clip);
        }

        private static Sprite[] LoadSprites(string path, int sourceFrameCount, int firstFrameIndex, int frameCount)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            System.Collections.Generic.List<Sprite> sprites = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            sprites.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            int expectedSourceCount = Mathf.Max(0, sourceFrameCount);
            if (sprites.Count > expectedSourceCount)
            {
                sprites.RemoveRange(expectedSourceCount, sprites.Count - expectedSourceCount);
            }

            int start = Mathf.Clamp(firstFrameIndex, 0, sprites.Count);
            int count = Mathf.Min(Mathf.Max(0, frameCount), sprites.Count - start);
            if (count <= 0)
            {
                return System.Array.Empty<Sprite>();
            }

            return sprites.GetRange(start, count).ToArray();
        }

        private static void BuildController()
        {
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            {
                AssetDatabase.DeleteAsset(ControllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(IsMovingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(Attack1Parameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(Attack2Parameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(Attack3Parameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(SuperSmashParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(DashParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HurtParameter, AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState idle = AddState(stateMachine, "Idle");
            AnimatorState run = AddState(stateMachine, "Run");
            AnimatorState dash = AddState(stateMachine, "Dash");
            AnimatorState hurt = AddState(stateMachine, "Hurt");
            AnimatorState attack1 = AddState(stateMachine, "Attack 1");
            AnimatorState attack2 = AddState(stateMachine, "Attack 2");
            AnimatorState attack3 = AddState(stateMachine, "Attack 3");
            AnimatorState superSmash = AddState(stateMachine, "Super Smash");
            stateMachine.defaultState = idle;

            AddBoolTransition(idle, run, IsMovingParameter, true);
            AddBoolTransition(run, idle, IsMovingParameter, false);
            AddTriggerTransition(stateMachine, dash, DashParameter);
            AddTriggerTransition(stateMachine, hurt, HurtParameter);
            AddTriggerTransition(stateMachine, superSmash, SuperSmashParameter);
            AddTriggerTransition(idle, attack1, Attack1Parameter);
            AddTriggerTransition(run, attack1, Attack1Parameter);
            AddTriggerTransition(idle, attack2, Attack2Parameter);
            AddTriggerTransition(run, attack2, Attack2Parameter);
            AddTriggerTransition(idle, attack3, Attack3Parameter);
            AddTriggerTransition(run, attack3, Attack3Parameter);
            AddComboTransition(attack1, attack2, Attack2Parameter);
            AddComboTransition(attack2, attack3, Attack3Parameter);
            AddExitTransition(dash, idle);
            AddExitTransition(hurt, idle);
            AddExitTransition(attack1, idle);
            AddExitTransition(attack2, idle);
            AddExitTransition(attack3, idle);
            AddExitTransition(superSmash, idle);

            EditorUtility.SetDirty(controller);
        }

        private static AnimatorState AddState(AnimatorStateMachine stateMachine, string stateName)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath(stateName));
            state.writeDefaultValues = false;
            return state;
        }

        private static void AddBoolTransition(AnimatorState fromState, AnimatorState toState, string parameterName, bool value)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameterName);
        }

        private static void AddTriggerTransition(AnimatorState fromState, AnimatorState toState, string parameterName)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
        }

        private static void AddTriggerTransition(AnimatorStateMachine stateMachine, AnimatorState toState, string parameterName)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(toState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
        }

        private static void AddComboTransition(AnimatorState fromState, AnimatorState toState, string parameterName)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);
        }

        private static void AddExitTransition(AnimatorState fromState, AnimatorState idleState)
        {
            AnimatorStateTransition transition = fromState.AddTransition(idleState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.duration = 0f;
            transition.canTransitionToSelf = false;
        }

        private static bool SetPlatform(TextureImporter importer, TextureImporterPlatformSettings settings, string platformName)
        {
            bool changed = false;
            changed |= SetImporterValue(!settings.overridden, () => settings.overridden = true);
            changed |= SetImporterValue(settings.maxTextureSize < 4096, () => settings.maxTextureSize = 4096);
            changed |= SetImporterValue(settings.format != TextureImporterFormat.RGBA32, () => settings.format = TextureImporterFormat.RGBA32);
            changed |= SetImporterValue(settings.textureCompression != TextureImporterCompression.Uncompressed, () => settings.textureCompression = TextureImporterCompression.Uncompressed);
            if (changed)
            {
                importer.SetPlatformTextureSettings(settings);
            }

            return changed;
        }

        private static bool SetImporterValue(bool shouldSet, System.Action apply)
        {
            if (!shouldSet)
            {
                return false;
            }

            apply();
            return true;
        }

        private static string SheetPath(string fileName)
        {
            return SpriteFolder + "/" + fileName;
        }

        private static string ClipPath(string stateName)
        {
            return AnimatorFolder + "/LittleAxion_" + stateName.Replace(" ", string.Empty) + ".anim";
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            int slash = folderPath.LastIndexOf('/');
            string parent = slash > 0 ? folderPath.Substring(0, slash) : "Assets";
            string folderName = folderPath.Substring(slash + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private readonly struct AxionAnimationDefinition
        {
            public readonly string StateName;
            public readonly string FileName;
            public readonly int FrameCount;
            public readonly int FirstFrameIndex;
            public readonly int SourceFrameCount;
            public readonly float FramesPerSecond;
            public readonly bool Loop;
            public readonly float HitEventNormalizedTime;

            public AxionAnimationDefinition(
                string stateName,
                string fileName,
                int frameCount,
                float framesPerSecond,
                bool loop,
                float hitEventNormalizedTime,
                int firstFrameIndex = 0,
                int sourceFrameCount = 0)
            {
                StateName = stateName;
                FileName = fileName;
                FrameCount = frameCount;
                FirstFrameIndex = Mathf.Max(0, firstFrameIndex);
                SourceFrameCount = sourceFrameCount > 0 ? sourceFrameCount : frameCount;
                FramesPerSecond = framesPerSecond;
                Loop = loop;
                HitEventNormalizedTime = hitEventNormalizedTime;
            }
        }
    }
}
