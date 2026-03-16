
namespace ET.Client
{
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(ET.AttributeComponent))]
    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AttributeComponent self)
        {
            self.AllModifiers = XList<DataModifier>.Create();
            self.Init(true);
        }

        public static void Init(this AttributeComponent self, bool isHero)
        {
            Unit unit = self.GetParent<Unit>();
            DRUnitAttribute unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(unit.ConfigId,self.Level);
            var num = unit.GetComponent<NumericComponent>();
            num.SetNoEvent(NumericType.MaxHpBase, self.GetValue(unitBaseConfig.HP));
            num.SetNoEvent(NumericType.HpBase, self.GetValue(unitBaseConfig.HP));
            num.SetNoEvent(NumericType.CriticalProbabilityBase, self.GetValue(unitBaseConfig.CriticalProbability));
            num.SetNoEvent(NumericType.ModeBase, self.GetValue(unitBaseConfig.Mode));
            num.SetNoEvent(NumericType.ModeMaxBase, self.GetValue(unitBaseConfig.Mode));
            num.SetNoEvent(NumericType.MpBase, self.GetValue(unitBaseConfig.MP));
            num.SetNoEvent(NumericType.MaxMpBase, self.GetValue(unitBaseConfig.MP));
            num.SetNoEvent(NumericType.AttackBase, self.GetValue(unitBaseConfig.Attack));
            num.SetNoEvent(NumericType.ArmorBase, self.GetValue(unitBaseConfig.Armor));
            num.SetNoEvent(NumericType.SpeedBase, unitBaseConfig.MoveSpeed);
            num.SetNoEvent(NumericType.AttackSpeedBase, self.GetValue(unitBaseConfig.AttackSpeed));
        }

        private static int GetValue(this AttributeComponent self, float v)
        {
            var ret = (int)v * 1000;
            return ret;
        }

        [EntitySystem]
        private static void Destroy(this AttributeComponent self)
        {
            self.AllModifiers.Dispose();
        }

        public static DataModifier AddModifier(this AttributeComponent self, int type, float value)
        {
            DataModifier modify = DataModifier.Create(self.DataId++, type, value);
            self.AllModifiers.Add(modify);
            self.CountAttr(modify.Attribute);
            return modify;
        }

        /// <summary>
        /// 删除某个buff
        /// </summary>
        /// <param name="self"></param>
        /// <param name="modify"></param>
        public static void RemoveModifer(this AttributeComponent self, DataModifier modify)
        {
            if (modify != null)
            {
                self.AllModifiers.Remove(modify);
                self.CountAttr(modify.Attribute);
                modify.Dispose();
            }
        }

        /// <summary>
        /// 统计所有属性
        /// </summary>
        /// <param name="self"></param>
        /// <param name="attributeType"></param>
        private static void CountAttr(this AttributeComponent self, int attributeType)
        {
            float value = 0;
            foreach (var v in self.AllModifiers)
            {
                if (v.Attribute == attributeType)
                {
                    value += v.Value;
                }
            }

            self.NumericComponent.Set(attributeType, value);
        }
    }
}