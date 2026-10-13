using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace YazLab.EditorTools
{
    /// <summary>Katmanlar, URP ve model içe aktarma gibi proje genel ayarlarını uygular.</summary>
    public static class ProjectSetup
    {
        public const int GroundLayer = 6;
        public const int ObstacleLayer = 7;
        public const int DynamicObstacleLayer = 8;
        public const int InteractableLayer = 9;
        public const int PlayerLayer = 10;
        public const int CompanionLayer = 11;
        public const int NpcLayer = 12;

        private const string RenderPipelineAssetPath = "Assets/_Project/Settings/PC_RPAsset.asset";
        private const string KenneyFolder = "Assets/ThirdParty/Kenney_PlatformerKit";

        [MenuItem("YazLab/1 - Proje Ayarlarını Uygula")]
        public static void Apply()
        {
            EnsureLayers();
            EnsureRenderPipeline();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.productName = "YazLab Macera";
            PlayerSettings.companyName = "KOU BSM";

            // Malzemelerin URP shader'ı ile yeniden üretilmesi için modelleri yeniden içe aktar.
            AssetDatabase.ImportAsset(KenneyFolder, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("[YazLab] Proje ayarları uygulandı.");
        }

        private static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");

            SetLayer(layers, GroundLayer, "Ground");
            SetLayer(layers, ObstacleLayer, "Obstacle");
            SetLayer(layers, DynamicObstacleLayer, "DynamicObstacle");
            SetLayer(layers, InteractableLayer, "Interactable");
            SetLayer(layers, PlayerLayer, "Player");
            SetLayer(layers, CompanionLayer, "Companion");
            SetLayer(layers, NpcLayer, "NPC");

            tagManager.ApplyModifiedProperties();
        }

        private static void SetLayer(SerializedProperty layers, int index, string name)
        {
            layers.GetArrayElementAtIndex(index).stringValue = name;
        }

        private static void EnsureRenderPipeline()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(RenderPipelineAssetPath);
            if (pipeline == null)
            {
                Debug.LogError($"[YazLab] URP asset bulunamadı: {RenderPipelineAssetPath}");
                return;
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            QualitySettings.SetQualityLevel(current, false);
        }
    }
}
