using UnityEditor;
using UnityEditor.SceneManagement;

namespace YazLab.EditorTools
{
    /// <summary>
    /// Editör boş/isimsiz bir sahneyle açılırsa ana oyun sahnesini otomatik açar.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenMainSceneOnLoad
    {
        private const string MainScenePath = "Assets/_Project/Scenes/Main.unity";
        private const string SessionKey = "YazLab.MainSceneOpened.v2";

        static OpenMainSceneOnLoad()
        {
            EditorApplication.delayCall += TryOpenMainScene;
        }

        private static void TryOpenMainScene()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            SessionState.SetBool(SessionKey, true);

            var active = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(active.path)) return;
            // İsimsiz sahne hiç kaydedilmemiştir; kaybedilecek içerik yok.

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath) != null)
                EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        }
    }
}
