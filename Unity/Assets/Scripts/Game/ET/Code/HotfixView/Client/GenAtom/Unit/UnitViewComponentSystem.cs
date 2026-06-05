namespace ET.Client
{
    [EntitySystemOf(typeof(UnitViewComponent))]
    [FriendOf(typeof(UnitViewComponent))]
    public static partial class UnitViewComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitViewComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UnitViewComponent self)
        {
            global::ET.UGFEntity viewEntity = self.ViewEntity.As();
            self.ViewEntity = default;
            if (viewEntity != null && !viewEntity.IsDisposed)
            {
                viewEntity.Dispose();
            }
        }

        public static void Bind(this UnitViewComponent self, global::ET.UGFEntity viewEntity)
        {
            self.ViewEntity = viewEntity;
        }
    }
}
