using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace YazLab.EditorTools
{
    /// <summary>Kenney karakter animasyonlarından Speed (blend tree) + Interact (trigger) kontrolcüsü üretir.</summary>
    public static class CharacterAnimatorBuilder
    {
        public const string ControllerPath = "Assets/_Project/Animations/KenneyCharacter.controller";
        private const string ClipSourcePath = "Assets/ThirdParty/Kenney_PlatformerKit/Models/character-oobi.fbx";

        [MenuItem("YazLab/Animator Kontrolcüsünü Oluştur")]
        public static AnimatorController Build()
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(ClipSourcePath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .ToDictionary(c => c.name);

            AnimationClip Clip(string name)
            {
                if (clips.TryGetValue(name, out var clip)) return clip;
                Debug.LogWarning($"[YazLab] Animasyon bulunamadı: {name}. Mevcut: {string.Join(", ", clips.Keys)}");
                return null;
            }

            AssetDatabase.DeleteAsset(ControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            controller.AddParameter("Interact", AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;

            var locomotion = controller.CreateBlendTreeInController("Locomotion", out var blendTree);
            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = "Speed";
            blendTree.useAutomaticThresholds = false;
            blendTree.AddChild(Clip("idle"), 0f);
            blendTree.AddChild(Clip("walk"), 2.5f);
            blendTree.AddChild(Clip("sprint"), 5.5f);
            stateMachine.defaultState = locomotion;

            var interact = stateMachine.AddState("Interact");
            interact.motion = Clip("interact-right");

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
