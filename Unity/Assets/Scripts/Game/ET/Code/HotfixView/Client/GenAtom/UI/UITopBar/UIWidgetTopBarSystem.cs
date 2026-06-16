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
            
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetTopBar self, bool isShutdown)
        {
            
        }
    }
}