using UnityEditor;

namespace Game.Editor
{
    public static class ManualRefreshTool
    {
        private const string AutoRefreshDisabledKey = "Game.Editor.ManualRefreshTool.AutoRefreshDisabled";

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EnsureEnterPlayModeOptions();
            EnsureAutoRefreshDisabled();
        }

        private static void EnsureEnterPlayModeOptions()
        {
            if (!EditorSettings.enterPlayModeOptionsEnabled)
            {
                EditorSettings.enterPlayModeOptionsEnabled = true;
            }

            if (EditorSettings.enterPlayModeOptions != EnterPlayModeOptions.DisableDomainReload)
            {
                EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;
            }
        }

        private static void EnsureAutoRefreshDisabled()
        {
            if (SessionState.GetBool(AutoRefreshDisabledKey, false))
            {
                return;
            }

            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool(AutoRefreshDisabledKey, true);
        }
    }
}
