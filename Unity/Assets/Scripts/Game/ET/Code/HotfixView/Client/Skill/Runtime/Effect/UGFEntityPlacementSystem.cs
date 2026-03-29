namespace ET.Client
{
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(UGFEntityPlacement))]
    [FriendOf(typeof(PlacementEffectSpec))]
    [EntitySystemOf(typeof(UGFEntityPlacement))]
    public static partial class UGFEntityPlacementSystem
    {
        [EntitySystem]
        private static void Awake(this UGFEntityPlacement self, PlacementInitData initData)
        {
            self.InitData = initData;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this UGFEntityPlacement self)
        {
            PlacementEffectSpec placementSpec = self.GetEffectSpec()?.GetComponent<PlacementEffectSpec>();
            if (placementSpec != null)
            {
                placementSpec.PlacementEntity = self;
            }

            self.SyncFromSpec();
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this UGFEntityPlacement self, bool isShutdown)
        {
            PlacementEffectSpec placementSpec = self.GetEffectSpec()?.GetComponent<PlacementEffectSpec>();
            if (placementSpec != null && placementSpec.PlacementEntity.As() == self)
            {
                placementSpec.PlacementEntity = default;
            }
        }

        [UGFEntitySystem]
        private static void UGFEntityOnUpdate(this UGFEntityPlacement self, float elapseSeconds, float realElapseSeconds)
        {
            self.SyncFromSpec();
        }

        public static void Cancel(this UGFEntityPlacement self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Dispose();
        }

        private static void SyncFromSpec(this UGFEntityPlacement self)
        {
            PlacementEffectSpec placementSpec = self.GetEffectSpec()?.GetComponent<PlacementEffectSpec>();
            if (placementSpec == null || self.CachedTransform == null)
            {
                return;
            }

            self.CachedTransform.position = placementSpec.RuntimePosition;
        }

        private static GameplayEffectSpec GetEffectSpec(this UGFEntityPlacement self)
        {
            return self?.GetParent<GameplayEffectSpec>();
        }
    }
}
