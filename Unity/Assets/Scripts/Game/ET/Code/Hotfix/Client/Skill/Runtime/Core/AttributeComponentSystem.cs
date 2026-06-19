namespace ET.Client
{
    /// <summary>
    /// AttributeComponent 的运行时逻辑。
    /// 负责按配置初始化基础属性与 AttrCmp 子实体。
    /// </summary>
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(NumericComponent))]
    [FriendOfAttribute(typeof(ET.Client.AttrCmp))]
    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AttributeComponent self, int configId, int level, int subLevel)
        {
            self.ConfigId = configId;
            self.Level = level;
            self.SubLevel = subLevel;
            self.Init(self.ConfigId, self.Level, self.SubLevel);
        }

        [EntitySystem]
        private static void Destroy(this AttributeComponent self)
        {
            self.Clear();
        }

        static void Init(this AttributeComponent self, int configId, int level, int subLevel)
        {
            NumericComponent numericComponent = self.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            DRUnitAttribute unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(configId, level, subLevel);
            if (unitBaseConfig != null)
            {
                numericComponent.SetNoEvent(NumericType.Hp, ToNumericLong(unitBaseConfig.HP));
                numericComponent.SetNoEvent(NumericType.MaxHp, ToNumericLong(unitBaseConfig.HP));
                numericComponent.SetNoEvent(NumericType.CriticalProbability, ToNumericLong(unitBaseConfig.CriticalProbability));
                numericComponent.SetNoEvent(NumericType.Mode, ToNumericLong(unitBaseConfig.Mode));
                numericComponent.SetNoEvent(NumericType.ModeMax, ToNumericLong(unitBaseConfig.Mode));
                numericComponent.SetNoEvent(NumericType.Mp, ToNumericLong(unitBaseConfig.MP));
                numericComponent.SetNoEvent(NumericType.MaxMp, ToNumericLong(unitBaseConfig.MP));
                numericComponent.SetNoEvent(NumericType.Attack, ToNumericLong(unitBaseConfig.Attack));
                numericComponent.SetNoEvent(NumericType.Armor, ToNumericLong(unitBaseConfig.Armor));
                numericComponent.SetNoEvent(NumericType.Speed, unitBaseConfig.MoveSpeed);
                numericComponent.SetNoEvent(NumericType.AttackSpeed, unitBaseConfig.AttackSpeed);
                numericComponent.SetNoEvent(NumericType.MaxAge, unitBaseConfig.MaxAge);
            }

            self.AddAttrCmps();
        }

        static void AddAttrCmps(this AttributeComponent self)
        {
            foreach (int numericType in NumericType.GetClientAttributeTypes())
            {
                self.GetOrAddChild<AttrCmp, int>(numericType, numericType);
            }
        }

        private static long ToNumericLong(float value)
        {
            return (long)(value * 10000);
        }
    }
}
