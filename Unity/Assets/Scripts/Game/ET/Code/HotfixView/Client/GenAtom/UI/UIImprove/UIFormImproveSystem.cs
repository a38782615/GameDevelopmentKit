namespace ET.Client
{
    [FriendOf(typeof(UIFormImprove))]
    [EntitySystemOf(typeof(UIFormImprove))]
    public static partial class UIFormImproveSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormImprove self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIFormImprove self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormImprove self)
        {
            
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormImprove self, bool isShutdown)
        {
            
        }
    }
}