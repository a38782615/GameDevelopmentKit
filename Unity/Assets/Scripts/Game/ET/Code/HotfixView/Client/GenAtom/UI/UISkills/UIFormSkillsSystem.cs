namespace ET.Client
{
    [FriendOf(typeof(UIFormSkills))]
    [EntitySystemOf(typeof(UIFormSkills))]
    public static partial class UIFormSkillsSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormSkills self)
        {
            
        }

        [EntitySystem]
        private static void Destroy(this UIFormSkills self)
        {
            
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormSkills self)
        {
            
        }
        
        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkills self, bool isShutdown)
        {
            
        }
    }
}