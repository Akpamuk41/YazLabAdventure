using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace YazLab.EditorTools
{
    /// <summary>Karakter animasyonlarından Speed (blend tree) + Interact (trigger) kontrolcüsü üretir.</summary>
    public static class CharacterAnimatorBuilder
    {
        public const string ControllerPath = "Assets/_Project/Animations/KenneyCharacter.controller";
        public const string KayKitControllerPath = "Assets/_Project/Animations/KayKitCharacter.controller";
        public const string PrincessControllerPath = "Assets/_Project/Animations/PrincessCharacter.controller";
        private const string ClipSourcePath = "Assets/ThirdParty/Kenney_PlatformerKit/Models/character-oobi.fbx";
        private const string KayKitClipSourcePath = "Assets/ThirdParty/KayKit_Adventurers/Models/Knight.fbx";
        private const string PrincessClipSourcePath = "Assets/ThirdParty/Quaternius_ModularWomen/Models/Medieval.fbx";

        [MenuItem("YazLab/Animator Kontrolcüsünü Oluştur")]
        public static AnimatorController Build()
        {
            Create(PrincessControllerPath, PrincessClipSourcePath, "Idle_Sword", "Walk", "Run", "Interact");
            Create(KayKitControllerPath, KayKitClipSourcePath, "Idle", "Walking_A", "Running_A", "Interact");
            return Create(ControllerPath, ClipSourcePath, "idle", "walk", "sprint", "interact-right");
        }

        /// <summary>Hareket kliplerinin döngü ayarını import ayarlarına kalıcı yazar (yoksa yürüme bir kez oynayıp donuyor).</summary>
        private static void EnsureLooping(string modelPath, params string[] loopingClips)
        {
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter importer)) return;

            var clips = importer.clipAnimations.Length > 0 ? importer.clipAnimations : importer.defaultClipAnimations;
            bool changed = importer.clipAnimations.Length == 0;
            foreach (var clip in clips)
            {
                string shortName = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                if (clip.name != shortName)
                {
                    clip.name = shortName;
                    changed = true;
                }

                bool loop = loopingClips.Contains(clip.name);
                if (clip.loopTime == loop) continue;
                clip.loopTime = loop;
                changed = true;
            }

            if (!changed) return;
            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        private static AnimatorController Create(string controllerPath, string clipSourcePath,
            string idle, string walk, string run, string interactClip)
        {
            EnsureLooping(clipSourcePath, idle, walk, run);

            var clips = AssetDatabase.LoadAllAssetsAtPath(clipSourcePath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name);

            AnimationClip Clip(string name)
            {
                if (clips.TryGetValue(name, out var clip)) return clip;
                Debug.LogWarning($"[YazLab] Animasyon bulunamadı: {name}. Mevcut: {string.Join(", ", clips.Keys)}");
                return null;
            }

            AssetDatabase.DeleteAsset(controllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var blendTree);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;
            blendTree.AddChild(Clip(idle), 0f);
            blendTree.AddChild(Clip(walk), 2.5f);
            blendTree.AddChild(Clip(run), 5.5f);
            stateMachine.defaultState = locomotion;

            var interact = stateMachine.AddState("Interact");
            interact.motion = Clip(interactClip);

            var toInteract = locomotion.AddTransition(interact);
            toInteract.AddCondition(AnimatorConditionMode.If, 0f, "Interact");
            toInteract.hasExitTime = false;
            toInteract.duration = 0.1f;

            var toLocomotion = interact.AddTransition(locomotion);
            toLocomotion.hasExitTime = true;
            toLocomotion.exitTime = 0.9f;
            toLocomotion.duration = 0.15f;

            AssetDatabase.SaveAssets();
            return controller;
        }
    }
}
