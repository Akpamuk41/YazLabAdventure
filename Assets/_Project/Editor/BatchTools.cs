using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Komut satırından (-executeMethod) çalıştırılabilen kurulum ve build adımları.
    /// Örnek: Unity.exe -batchmode -quit -projectPath . -executeMethod YazLab.EditorTools.BatchTools.SetupAll
    /// </summary>
    public static class BatchTools
    {
        public const string WindowsBuildPath = "Builds/Windows/YazLabMacera.exe";

        [MenuItem("YazLab/Hepsini Kur (Ayarlar + Animator + Seviye)")]
        public static void SetupAll()
        {
            ProjectSetup.Apply();
            CharacterAnimatorBuilder.Build();
            LevelBuilder.Build();

            string screenshotDir = GetArgument("-screenshotDir");
            if (!string.IsNullOrEmpty(screenshotDir)) LevelBuilder.CaptureScreenshots(screenshotDir);
        }

        // Play modunda sahne yeniden kurulamaz; menüler soluk görünür.
        [MenuItem("YazLab/Hepsini Kur (Ayarlar + Animator + Seviye)", true)]
        [MenuItem("YazLab/Windows Build Al", true)]
        [MenuItem("YazLab/2 - Seviyeyi Oluştur", true)]
        [MenuItem("YazLab/Ekran Görüntülerini Al", true)]
        public static bool NotInPlayMode() => !EditorApplication.isPlayingOrWillChangePlaymode;

        public static void CaptureScreenshots()
        {
            EditorSceneManager.OpenScene(LevelBuilder.ScenePath);
            LevelBuilder.CaptureScreenshots(GetArgument("-screenshotDir") ?? Path.GetFullPath("Screenshots"));
        }

        [MenuItem("YazLab/Windows Build Al")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[YazLab] Build sonucu: {report.summary.result}, boyut {report.summary.totalSize / (1024 * 1024)} MB");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        private static string GetArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, name);
            return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
        }
    }
}
