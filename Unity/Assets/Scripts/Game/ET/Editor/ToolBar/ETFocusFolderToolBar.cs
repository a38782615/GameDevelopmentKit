using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace ET.Editor
{
    public static class ETFocusFolderToolBar
    {
        [ToolbarButton(OnGUISide.Left, -20, "ETUI-Model", "Focus UI Model Code Folder.")]
        private static void FocusUIModelCodeFolder()
        {
            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Scripts/Game/ET/Code/ModelView/Client/Game/UI");
            Selection.activeObject = obj;
        }

        [ToolbarButton(OnGUISide.Left, -19, "ETUI-Hotfix", "Focus UI Hotfix Code Folder.")]
        private static void FocusUIHotfixCodeFolder()
        {
            EditorUtility.FocusProjectWindow();
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Scripts/Game/ET/Code/HotfixView/Client/Game/UI");
            Selection.activeObject = obj;
        }
    }
}
