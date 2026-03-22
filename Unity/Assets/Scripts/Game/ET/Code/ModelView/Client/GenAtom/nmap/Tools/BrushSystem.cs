namespace ET
{
    [FriendOf(typeof(Brush))]
    [EntitySystemOf(typeof(Brush))]
    public static partial class BrushSystem
    {
        [EntitySystem]
        private static void Awake(this Brush self)
        {
        }
    }
}
