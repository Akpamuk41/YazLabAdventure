using System.Linq;
using UnityEditor;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Kenney, KayKit ve Quaternius modellerinin içe aktarma ayarları: karakterler Generic rig + döngülü hareket animasyonları,
    /// diğer modeller animasyonsuz (şalter/kapı hareketini kod yapıyor).
    /// </summary>
    public class KenneyModelPostprocessor : AssetPostprocessor
    {
        private const string KenneyFolder = "Assets/ThirdParty/Kenney_PlatformerKit";
        private const string KayKitFolder = "Assets/ThirdParty/KayKit_Adventurers";
        private const string QuaterniusFolder = "Assets/ThirdParty/Quaternius_ModularWomen";
        private const string CastleFolder = "Assets/ThirdParty/Kenney_CastleKit";
        private static readonly string[] LoopingClips =
        {
            "idle", "walk", "sprint", "static", "holding-both", "holding-left", "holding-right",
            "Idle", "Walking_A", "Running_A", "Idle_Sword", "Walk", "Run"
        };

        private bool IsKenney => assetPath.StartsWith(KenneyFolder);
        private bool IsKayKit => assetPath.StartsWith(KayKitFolder);
        private bool IsQuaternius => assetPath.StartsWith(QuaterniusFolder);
        private bool IsCastle => assetPath.StartsWith(CastleFolder);
        private bool IsOurs => IsKenney || IsKayKit || IsQuaternius || IsCastle;

        private bool IsCharacter
        {
            get
            {
                if (IsQuaternius) return true;
                if (IsCastle) return false;
                var file = System.IO.Path.GetFileName(assetPath);
                return IsKayKit ? char.IsUpper(file[0]) : file.StartsWith("character-");
            }
        }

        private void OnPreprocessModel()
        {
            if (!IsOurs) return;

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
            if (!IsOurs || !IsCharacter) return;

            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                // Quaternius klipleri "CharacterArmature|Walk" gibi geliyor, öneki at.
                clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                clip.loopTime = LoopingClips.Contains(clip.name);
            }

            importer.clipAnimations = clips;
        }
    }
}
