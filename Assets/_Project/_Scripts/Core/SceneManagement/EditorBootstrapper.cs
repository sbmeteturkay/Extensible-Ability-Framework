// Core/SceneManagement/EditorBootstrapper.cs
// SADECE editörde çalışır — build'e dahil olmaz

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace SabanCoreTemplate.SceneManagement
{
    [InitializeOnLoad]
    public static class EditorBootstrapper
    {
        private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";

        static EditorBootstrapper()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingEditMode) return;

            // Zaten Boot sahnesindeyse dokunma
            if (SceneManager.GetActiveScene().path == BootScenePath) return;

            // Kaydedilmemiş değişiklik varsa sor
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // İptal ettiyse play mode'a girme
                EditorApplication.isPlaying = false;
                return;
            }

            // Boot sahnesini aç, play mode zaten başlıyor
            EditorSceneManager.OpenScene(BootScenePath);
        }
    }
}
#endif