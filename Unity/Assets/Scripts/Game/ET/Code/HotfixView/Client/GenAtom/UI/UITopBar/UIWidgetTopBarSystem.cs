using Sirenix.OdinInspector;

namespace ET.Client
{
    [FriendOf(typeof(UIWidgetTopBar))]
    [EntitySystemOf(typeof(UIWidgetTopBar))]
    public static partial class UITopBarSystem
    {
        [EntitySystem]
        private static void Awake(this UIWidgetTopBar self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIWidgetTopBar self)
        {
            
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetTopBar self)
        {
            self.RefreshView();
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetTopBar self, bool isShutdown)
        {
            
        }

        private static void RefreshView(this UIWidgetTopBar self)
        {
            PlayerData playerData = self.Root()?.GetComponent<GameDataMgrComponent>()?.GetPlayerDataComponent()?.PlayerData;
            if (playerData == null)
            {
                return;
            }

            var maxAge = Tables.Instance.DTUnitAttribute.Get(playerData.ConfigId, playerData.Level, playerData.SubLevel).MaxAge;

            self.View.AgeUXTextMeshPro.text = $"{playerData.Age.ToString()}/{maxAge}";

            self.View.LevelUXTextMeshPro.text = LocalizationHelper.GetString($"Level_{playerData.Level}_{playerData.SubLevel}");
            
            self.View.StoneCountUXTextMeshPro.text = playerData.Diamond.ToString();
        }
    }
}
