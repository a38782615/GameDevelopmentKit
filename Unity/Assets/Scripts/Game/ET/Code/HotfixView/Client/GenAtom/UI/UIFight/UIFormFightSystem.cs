namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormFight self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIFormFight self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
            
        }
    }
}