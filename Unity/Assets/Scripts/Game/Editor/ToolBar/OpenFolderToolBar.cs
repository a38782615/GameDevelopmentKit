using ToolbarExtension;
using UnityEngine;

namespace Game.Editor
{
    sealed class OpenFolderToolBar
    {
        [ToolbarButton(OnGUISide.Right, -1, "Open-Excel", "Open Excel Folder!")]
        private static void OpenExcelFolder()
        {
            OpenFolderTool.OpenExcelPath();
        }

        [ToolbarButton(OnGUISide.Right, -2, "Open-Proto", "Open Proto Folder!")]
        private static void OpenProtoFolder()
        {
            OpenFolderTool.OpenProtoPath();
        }

        [ToolbarButton(OnGUISide.Right, -3, "Open-Build", "Open Build Folder!")]
        private static void OpenBuildFolder()
        {
            OpenFolderTool.OpenBuildPath();
        }
    }
}
