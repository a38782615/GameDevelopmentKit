namespace ET
{
    [FriendOf(typeof(DrawMap))]
    [EntitySystemOf(typeof(DrawMap))]
    public static partial class DrawMapSystem
    {
        [EntitySystem]
        private static void Awake(this DrawMap self)
        {
        }
    }
}
