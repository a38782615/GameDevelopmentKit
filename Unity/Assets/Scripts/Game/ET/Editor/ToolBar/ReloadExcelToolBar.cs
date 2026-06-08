using Cysharp.Threading.Tasks;
using ToolbarExtension;
using UnityEditor;
using UnityEngine;

namespace ET.Editor
{
    sealed class ReloadExcelToolBar
    {
        private static bool s_IsReloading = false;

        [ToolbarButton(OnGUISide.Right, 98, "ReloadExcel", "Reload (No Export) All Excel!")]
        private static void ReloadExcel()
        {
            if (!Application.isPlaying || s_IsReloading)
            {
                return;
            }

            s_IsReloading = true;

            async UniTaskVoid ReloadAsync()
            {
                try
                {
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    await ConfigComponent.Instance.ReloadAllAsync();
                    Debug.Log("Export And Reload All Excel!");
                }
                finally
                {
                    s_IsReloading = false;
                }
            }

            ReloadAsync().Forget();
        }
    }
}
