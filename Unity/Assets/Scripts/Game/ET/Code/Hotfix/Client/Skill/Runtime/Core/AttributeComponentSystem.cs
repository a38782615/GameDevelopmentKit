using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// AttributeComponent 的运行时逻辑。
    /// 负责按 NumericComponent 初始化/同步 AttrCmp 子实体，并派发属性变化事件。
    /// </summary>
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(global::ET.AttributeComponent))]
    [FriendOfAttribute(typeof(global::ET.NumericComponent))]
    [FriendOfAttribute(typeof(ET.Client.AttrCmp))]

    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this global::ET.AttributeComponent self, int configId, int level, int subLevel)
        {
            self.AllModifiers = XList<DataModifier>.Create();
            self.ConfigId = configId;
            self.Level = level;
            self.SubLevel = subLevel;
            self.Init(self.ConfigId, self.Level, self.SubLevel);
        }

        [EntitySystem]
        private static void Destroy(this global::ET.AttributeComponent self)
        {
            self.Clear();
            self.AllModifiers?.Dispose();
        }

        public static void Init(this global::ET.AttributeComponent self, int configId, int level, int subLevel)
        {
            NumericComponent numericComponent = self.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            var unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(configId, level, subLevel);
            if (unitBaseConfig != null)
            {
                numericComponent.SetNoEvent(NumericType.MaxHp, ToNumericLong(unitBaseConfig.HP));
                numericComponent.SetNoEvent(NumericType.Hp, ToNumericLong(unitBaseConfig.HP));
                numericComponent.SetNoEvent(NumericType.CriticalProbability, ToNumericLong(unitBaseConfig.CriticalProbability));
                numericComponent.SetNoEvent(NumericType.Mode, ToNumericLong(unitBaseConfig.Mode));
                numericComponent.SetNoEvent(NumericType.ModeMax, ToNumericLong(unitBaseConfig.Mode));
                numericComponent.SetNoEvent(NumericType.Mp, ToNumericLong(unitBaseConfig.MP));
                numericComponent.SetNoEvent(NumericType.MaxMp, ToNumericLong(unitBaseConfig.MP));
                numericComponent.SetNoEvent(NumericType.Attack, ToNumericLong(unitBaseConfig.Attack));
                numericComponent.SetNoEvent(NumericType.Armor, ToNumericLong(unitBaseConfig.Armor));
                numericComponent.SetNoEvent(NumericType.Speed, unitBaseConfig.MoveSpeed);
                numericComponent.SetNoEvent(NumericType.AttackSpeed, ToNumericLong(unitBaseConfig.AttackSpeed));
            }

            self.RefreshRuntimeAttributesFromNumeric();
        }

        public static AttrCmp AddAttribute(this global::ET.AttributeComponent self, int numericType, float defaultValue = 0f)
        {
            AttrCmp existing = self.GetAttrCmp(numericType);
            if (existing != null)
            {
                return existing;
            }

            // 子实体 Id 直接使用 NumericType，便于快速定位。
            AttrCmp attribute = self.AddChildWithId<AttrCmp, int>(numericType, numericType);
            return attribute;
        }

        public static void RefreshRuntimeAttributesFromNumeric(this global::ET.AttributeComponent self)
        {
            // 统一从 NumericComponent 拉取客户端关心的属性，构建或刷新 AttrCmp。
            foreach (int numericType in global::ET.NumericType.GetClientAttributeTypes())
            {
                RefreshRuntimeAttributeFromNumeric(self, numericType);
            }
        }

        //添加额外属性
        public static DataModifier AddModifier(this global::ET.AttributeComponent self, int type, float value)
        {
            DataModifier modify = DataModifier.Create(self.DataId++, type, value);
            self.AllModifiers.Add(modify);
            CountAttr(self, modify.Attribute);
            return modify;
        }

        //移除额外属性
        public static void RemoveModifer(this global::ET.AttributeComponent self, DataModifier modify)
        {
            if (modify == null)
            {
                return;
            }

            self.AllModifiers.Remove(modify);
            CountAttr(self, modify.Attribute);
            modify.Dispose();
        }

        private static void RefreshRuntimeAttributeFromNumeric(global::ET.AttributeComponent self, int numericType)
        {
            float value = self.NumericComponent?.GetAsFloat(numericType) ?? 0f;
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                self.AddAttribute(numericType, value);
            }
        }


        private static long ToNumericLong(float value)
        {
            return (long)(value * 10000);
        }

        private static void CountAttr(global::ET.AttributeComponent self, int attributeType)
        {
            if (self.NumericComponent == null)
            {
                return;
            }

            float value = 0f;
            foreach (DataModifier modifier in self.AllModifiers)
            {
                if (modifier.Attribute == attributeType)
                {
                    value += modifier.Value;
                }
            }

            self.NumericComponent.Set(attributeType, value);
        }
    }
}
