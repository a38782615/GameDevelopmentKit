using System.IO;
using System.Linq;
using ToolbarExtension;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityGameFramework.Extension.Editor;

namespace Game.Editor
{
    internal sealed class SceneToolBar
    {
        private static string[] s_SceneNames;
        private static string[] s_SceneGuids;

        [ToolbarDropdown(OnGUISide.Left, -999, "Scene", "Open scene from Assets/Res.")]
        private static void PopulateSceneMenu(GenericMenu menu)
        {
            RefreshSceneCache();

            for (int i = 0; i < s_SceneNames.Length; i++)
            {
                int sceneIndex = i;
                menu.AddItem(new UnityEngine.GUIContent(s_SceneNames[i]), false, () => OpenScene(sceneIndex));
            }
        }

        private static void RefreshSceneCache()
        {
            var sceneList = AssetDatabase.FindAssets("t:scene", new[] { "Assets/Res" }).ToList();
            sceneList.Insert(0, AssetDatabase.AssetPathToGUID(EntryUtility.LauncherScenePath));
            s_SceneGuids = sceneList.ToArray();
            s_SceneNames = new string[s_SceneGuids.Length];

            for (int i = 0; i < s_SceneNames.Length; i++)
            {
                s_SceneNames[i] = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(s_SceneGuids[i]));
            }
        }

        private static void OpenScene(int selectedSceneIndex)
        {
            if (selectedSceneIndex < 0 || selectedSceneIndex >= s_SceneGuids.Length)
            {
                return;
            }

            if (SceneManager.GetActiveScene().isDirty)
            {
                if (EditorUtility.DisplayDialog("Scene", "The scene is not saved, do you want to save it?", "Save", "Cancel"))
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

            EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(s_SceneGuids[selectedSceneIndex]));
            s_SceneNames = null;
            s_SceneGuids = null;
        }
    }
}
