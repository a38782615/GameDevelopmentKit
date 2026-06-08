using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class FocusFolderToolBar
    {
        [ToolbarButton(OnGUISide.Left, -10, "UI-Res", "Focus UI Res Folder.")]
        private static void FocusUIResFolder()
        {
            EditorUtility.FocusProjectWindow();
            Object obj = AssetDatabase.LoadAssetAtPath<Object>("Assets/Res/UI/UIForm");
            Selection.activeObject = obj;
        }
    }
}
