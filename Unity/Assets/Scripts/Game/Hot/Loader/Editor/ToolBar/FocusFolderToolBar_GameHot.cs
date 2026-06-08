using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace Game.Hot.Editor
{
    public static class FocusFolderToolBar_GameHot
    {
        [ToolbarButton(OnGUISide.Left, -30, "Hot-Runtime", "Focus Hot Code Runtime Folder.")]
        private static void FocusHotRuntimeFolder()
        {
            EditorUtility.FocusProjectWindow();
            Object obj = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/Game/Hot/Code");
            Selection.activeObject = obj;
        }

        [ToolbarButton(OnGUISide.Left, -29, "Hot-UI", "Focus Hot Code UI Folder.")]
        private static void FocusHotUIFolder()
        {
            EditorUtility.FocusProjectWindow();
            Object obj = AssetDatabase.LoadAssetAtPath<Object>("Assets/Scripts/Game/Hot/Code/UI");
            Selection.activeObject = obj;
        }
    }
}
