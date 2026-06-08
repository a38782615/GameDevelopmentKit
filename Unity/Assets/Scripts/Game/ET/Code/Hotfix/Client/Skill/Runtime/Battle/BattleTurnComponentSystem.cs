namespace ET.Client
{
    [EntitySystemOf(typeof(BattleTurnComponent))]
    [FriendOf(typeof(BattleTurnComponent))]
    public static partial class BattleTurnComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BattleTurnComponent self)
        {
            self.ActiveAttackSpecs.Clear();
        }

        [EntitySystem]
        private static void Destroy(this BattleTurnComponent self)
        {
            self.ActiveAttackSpecs.Clear();
        }

        public static bool IsAttackInProgress(this BattleTurnComponent self)
        {
            return self != null && self.ActiveAttackSpecs.Count > 0;
        }

        public static BattleTurnComponent GetBattleTurnComponent(this Entity entity)
        {
            return entity?.Root()?.CurrentScene()?.GetComponent<BattleTurnComponent>();
        }

        public static bool IsAnyAttackInProgress(this Entity entity)
        {
            return entity.GetBattleTurnComponent().IsAttackInProgress();
        }
    }
}
