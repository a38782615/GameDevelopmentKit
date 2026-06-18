namespace ET
{
    [EntitySystemOf(typeof(Unit))]
    public static partial class UnitSystem
    {
        [EntitySystem]
        private static void Awake(this Unit self, int configId)
        {
            self.ConfigId = configId;
        }

        public static DRUnitConfig Config(this Unit self)
        {
            return Tables.Instance.DTUnitConfig.Get(self.ConfigId);
        }

        public static string Icon(this Unit self)
        {
            var ret = "";
            if (self.Type() == UnitType.Player)
            {
                ret = Tables.Instance.DTHero.Get(self.Id).HeadIcon;
            }
            else
            {
                ret = Tables.Instance.DTMonster.Get(self.Id).HeadIcon;
            }
            return ret;
        }

        public static UnitType Type(this Unit self)
        {
            return (UnitType)self.Config().Type;
        }
    }
}