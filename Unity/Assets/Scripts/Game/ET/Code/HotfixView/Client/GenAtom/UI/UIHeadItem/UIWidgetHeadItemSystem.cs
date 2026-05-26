namespace ET.Client
{
    [FriendOf(typeof(UIWidgetHeadItem))]
    [EntitySystemOf(typeof(UIWidgetHeadItem))]
    public static partial class UIHeadItemSystem
    {
        [EntitySystem]
        private static void Awake(this UIWidgetHeadItem self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIWidgetHeadItem self)
        {
            
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetHeadItem self)
        {
            
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetHeadItem self, bool isShutdown)
        {
            
        }
    }
}