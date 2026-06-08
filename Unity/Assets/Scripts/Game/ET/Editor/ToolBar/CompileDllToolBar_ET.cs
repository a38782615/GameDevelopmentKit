#if UNITY_HOTFIX
using Cysharp.Threading.Tasks;
using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace ET.Editor
{
    sealed class CompileDllToolBar_ET
    {
        private static bool s_IsReloading = false;

        [ToolbarButton(OnGUISide.Left, 0, "ETReload", "Compile And Reload ET.Hotfix Dll When Playing.")]
        private static void BuildReloadHotfix()
        {
            if (!Application.isPlaying || s_IsReloading)
            {
                return;
            }

            BuildAssemblyTool.Build();
            Debug.Log("compile success!");

            s_IsReloading = true;

            async UniTaskVoid ReloadAsync()
            {
                try
                {
                    await CodeLoaderComponent.Instance.ReloadAsync();
                    Debug.Log("reload hotfix success!");
                }
                finally
                {
                    s_IsReloading = false;
                }
            }

            ReloadAsync().Forget();
        }

        [ToolbarButton(OnGUISide.Left, 1, "ETCompile", "Compile All ET Dll.")]
        private static void BuildHotfixModel()
        {
            BuildAssemblyTool.Build();
            Debug.Log("compile success!");
        }
    }
}
#endif
