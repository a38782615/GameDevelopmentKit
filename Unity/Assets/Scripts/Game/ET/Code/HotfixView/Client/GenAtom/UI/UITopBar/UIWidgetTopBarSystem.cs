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

            self.View.AgeUXTextMeshPro.text = playerData.Age.ToString();
            self.View.LevelUXTextMeshPro.text = playerData.Level.ToString();
            self.View.StoneCountUXTextMeshPro.text = playerData.Diamond.ToString();
        }
    }
}
