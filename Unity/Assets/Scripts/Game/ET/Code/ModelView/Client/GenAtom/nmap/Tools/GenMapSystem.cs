namespace ET
{
    [FriendOf(typeof(GenMap))]
    [EntitySystemOf(typeof(GenMap))]
    public static partial class GenMapSystem
    {
        [EntitySystem]
        private static void Awake(this GenMap self)
        {
        }
    }
}
