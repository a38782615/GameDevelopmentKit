namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormMain))]
    [FriendOf(typeof(UIFormMain))]
    public static partial class UIFormMainComponentSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormMain self)
        {
            self.OpenAllUIWidgets();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMain self, bool isShutdown)
        {
        }
    }
}
