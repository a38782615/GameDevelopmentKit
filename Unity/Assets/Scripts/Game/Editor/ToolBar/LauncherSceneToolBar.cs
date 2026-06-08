using ToolbarExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Extension.Editor;

namespace Game.Editor
{
    internal sealed class LauncherSceneToolBar
    {
        [ToolbarButton(OnGUISide.Left, 100, "Launcher", "Start Run Launcher Scene.")]
        private static void StartLauncherScene()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            BuildSceneSetting.AllScenes();
            SceneHelper.StartScene(EntryUtility.LauncherScenePath);
        }
    }

    internal static class SceneHelper
    {
        private const string UnityEditorSceneToOpenKey = "UnityEditorSceneToOpen";
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            if (EditorPrefs.HasKey(UnityEditorSceneToOpenKey))
            {
                string scenePath = EditorPrefs.GetString(UnityEditorSceneToOpenKey);
                if (!SceneManager.GetActiveScene().path.Equals(scenePath))
                {
                    SceneManager.LoadScene(scenePath);
                }
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            if (EditorPrefs.HasKey(UnityEditorSceneToOpenKey))
            {
                EditorPrefs.DeleteKey(UnityEditorSceneToOpenKey);
            }
        }

        public static void StartScene(string scenePathName)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }
            EditorPrefs.SetString(UnityEditorSceneToOpenKey, scenePathName);
            EditorApplication.isPlaying = true;
        }
    }
}
