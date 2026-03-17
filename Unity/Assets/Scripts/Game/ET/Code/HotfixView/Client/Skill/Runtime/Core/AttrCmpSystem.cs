namespace ET.Client
{
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
