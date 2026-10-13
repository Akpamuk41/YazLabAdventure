using System.Linq;
using UnityEditor;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Kenney modellerinin içe aktarma ayarları: karakterler Generic rig + döngülü hareket animasyonları,
    /// diğer modeller animasyonsuz (şalter/kapı hareketini kod yapıyor).
    /// </summary>
    public class KenneyModelPostprocessor : AssetPostprocessor
    {
        private const string KenneyFolder = "Assets/ThirdParty/Kenney_PlatformerKit";
        private static readonly string[] LoopingClips = { "idle", "walk", "sprint", "static", "holding-both", "holding-left", "holding-right" };

        private bool IsKenney => assetPath.StartsWith(KenneyFolder);
        private bool IsCharacter => System.IO.Path.GetFileName(assetPath).StartsWith("character-");

        private void OnPreprocessModel()
        {
            if (!IsKenney) return;

            var importer = (ModelImporter)assetImporter;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;

            if (IsCharacter)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
            }
            else
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
            }
        }

        private void OnPreprocessAnimation()
        {
            if (!IsKenney || !IsCharacter) return;

            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = LoopingClips.Contains(clip.name);
            }

            importer.clipAnimations = clips;
        }
    }
}
