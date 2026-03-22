namespace ET
{
    [FriendOf(typeof(MapLogic))]
    [EntitySystemOf(typeof(MapLogic))]
    public static partial class MapLogicSystem
    {
        [EntitySystem]
        private static void Awake(this MapLogic self)
        {
        }
    }
}
