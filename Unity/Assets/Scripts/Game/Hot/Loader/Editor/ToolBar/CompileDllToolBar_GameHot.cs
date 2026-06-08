#if UNITY_HOTFIX
using ToolbarExtension;
using UnityEngine;

namespace Game.Hot.Editor
{
    internal sealed class CompileDllToolBar_GameHot
    {
        [ToolbarButton(OnGUISide.Left, 50, "HotCompile", "Compile GameHot Dll.")]
        private static void BuildHotDll()
        {
            BuildAssemblyTool.Build();
        }
    }
}
#endif
