namespace ET.Client
{
    [FriendOf(typeof(GFEntityDropItem))]
    [EntitySystemOf(typeof(GFEntityDropItem))]
    public static partial class GFEntityDropItemSystem
    {
        [EntitySystem]
        private static void Awake(this GFEntityDropItem self)
        {
           
        }

        [EntitySystem]
        private static void Destroy(this GFEntityDropItem self)
        {
            
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this GFEntityDropItem self)
        {
           
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this GFEntityDropItem self, bool isShutdown)
        {
           
        }
    }
}