namespace ET.Client
{
    [FriendOf(typeof(GFEntityHeadItem))]
    [EntitySystemOf(typeof(GFEntityHeadItem))]
    public static partial class GFEntityHeadItemSystem
    {
        [EntitySystem]
        private static void Awake(this GFEntityHeadItem self)
        {
           
        }

        [EntitySystem]
        private static void Destroy(this GFEntityHeadItem self)
        {
            
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this GFEntityHeadItem self)
        {
           
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this GFEntityHeadItem self, bool isShutdown)
        {
           
        }
    }
}