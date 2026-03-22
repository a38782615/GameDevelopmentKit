namespace ET
{
    [FriendOf(typeof(DrawCarpet))]
    [EntitySystemOf(typeof(DrawCarpet))]
    public static partial class DrawCarpetSystem
    {
        [EntitySystem]
        private static void Awake(this DrawCarpet self)
        {
        }
    }
}
