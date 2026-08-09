namespace ET.Client
{
    [FriendOf(typeof(UIFormShop))]
    [EntitySystemOf(typeof(UIFormShop))]
    public static partial class UIFormShopSystem
    { 
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormShop self)
        {
            self.OpenAllUIWidgets();
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormShop self, bool isShutdown)
        {
            
        }
    }
}