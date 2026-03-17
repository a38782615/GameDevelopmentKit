namespace ET.Client
{
    /// <summary>
    /// AttrCmp 的生命周期系统。
    /// Awake 时绑定 NumericType，Destroy 时清理运行时回调和 modifier。
    /// </summary>
    [EntitySystemOf(typeof(AttrCmp))]
    [FriendOf(typeof(AttrCmp))]
    public static partial class AttrCmpSystem
    {
        [EntitySystem]
        private static void Awake(this AttrCmp self, int numericType)
        {
            self.SetNumericType(numericType);
            self.Initialize(self.CurrentValue);
        }

        [EntitySystem]
        private static void Destroy(this AttrCmp self)
        {
            self.ClearCallbacks();
            self.ClearModifiers();
        }
    }
}
