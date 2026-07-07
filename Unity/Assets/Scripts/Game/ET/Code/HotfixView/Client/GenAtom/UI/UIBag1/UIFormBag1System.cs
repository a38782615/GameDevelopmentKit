namespace ET.Client
{
    [FriendOf(typeof(UIFormBag1))]
    [EntitySystemOf(typeof(UIFormBag1))]
    public static partial class UIFormBag1System
    {
        [EntitySystem]
        private static void Awake(this UIFormBag1 self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UIFormBag1 self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormBag1 self)
        {
            self.View.Grid0CommonLoopScrollRect.numItems = 8;
            self.View.Grid1CommonLoopScrollRect.numItems = 8;
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormBag1 self, bool isShutdown)
        {
            
        }
    }
}