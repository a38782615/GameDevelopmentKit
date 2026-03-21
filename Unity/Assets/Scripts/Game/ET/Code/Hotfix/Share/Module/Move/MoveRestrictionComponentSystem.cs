namespace ET
{
    [EntitySystemOf(typeof(MoveRestrictionComponent))]
    [FriendOf(typeof(MoveRestrictionComponent))]
    public static partial class MoveRestrictionComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MoveRestrictionComponent self)
        {
            self.IsBlocked = false;
        }

        public static bool IsMoveAllowed(this MoveRestrictionComponent self)
        {
            return self == null || !self.IsBlocked;
        }

        public static bool SetBlocked(this MoveRestrictionComponent self, bool isBlocked)
        {
            if (self == null || self.IsBlocked == isBlocked)
            {
                return false;
            }

            self.IsBlocked = isBlocked;
            return true;
        }
    }
}
