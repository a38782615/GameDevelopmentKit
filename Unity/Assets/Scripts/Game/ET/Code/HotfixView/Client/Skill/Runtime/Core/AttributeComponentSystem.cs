using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(ET.AttributeComponent))]
    [FriendOfAttribute(typeof(ET.NumericComponent))]
    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AttributeComponent self)
        {
            self.AllModifiers = XList<DataModifier>.Create();
            self.RuntimeAttributes = new AttributeSetContainer();
            self.BindRuntimeAttributeEvents();
            self.RefreshRuntimeAttributesFromNumeric(false);
        }

        public static void Init(this AttributeComponent self, bool isHero)
        {
            Unit unit = self.GetParent<Unit>();
            DRUnitAttribute unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(unit.ConfigId, self.Level);
            NumericComponent num = unit.GetComponent<NumericComponent>();
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
            self.RefreshRuntimeAttributesFromNumeric(true);
        }

        public static AttributeSetContainer GetRuntimeAttributes(this AttributeComponent self)
        {
            return self?.RuntimeAttributes as AttributeSetContainer;
        }

        public static void RefreshRuntimeAttributesFromNumeric(this AttributeComponent self, bool overwriteExisting)
        {
            AttributeSetContainer runtimeAttributes = self.GetRuntimeAttributes();
            NumericComponent numericComponent = self?.NumericComponent;
            if (runtimeAttributes == null || numericComponent?.NumericDic == null)
            {
                return;
            }

            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.Health, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.MaxHealth, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.HealthRegen, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.Mana, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.MaxMana, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.ManaRegen, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.Attack, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.Defense, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.MagicPower, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.MagicDefense, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.MoveSpeed, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.AttackSpeed, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.CooldownReduction, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.CritRate, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.CritDamage, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(runtimeAttributes, numericComponent, AttrType.Level, overwriteExisting);
        }

        public static void InitializeMissingRuntimeAttributesFromConfig(this AttributeComponent self)
        {
            AttributeSetContainer runtimeAttributes = self.GetRuntimeAttributes();
            Unit unit = self.GetParent<Unit>();
            if (runtimeAttributes == null || unit == null)
            {
                return;
            }

            int level = self.Level > 0 ? self.Level : 1;
            DRUnitAttribute unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(unit.ConfigId, level);
            if (unitBaseConfig == null && level != 0)
            {
                unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(unit.ConfigId, 0);
            }

            if (unitBaseConfig == null)
            {
                return;
            }

            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.Health, unitBaseConfig.HP);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.MaxHealth, unitBaseConfig.HP);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.Mana, unitBaseConfig.MP);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.MaxMana, unitBaseConfig.MP);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.Attack, unitBaseConfig.Attack);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.Defense, unitBaseConfig.Armor);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.MoveSpeed, unitBaseConfig.MoveSpeed / 10000f);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.AttackSpeed, unitBaseConfig.AttackSpeed);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.CritRate, unitBaseConfig.CriticalProbability);
            AddRuntimeAttributeIfMissing(runtimeAttributes, AttrType.Level, level);

            self.SyncAllRuntimeAttributesToNumeric();
        }

        public static void SyncAllRuntimeAttributesToNumeric(this AttributeComponent self)
        {
            AttributeSetContainer runtimeAttributes = self.GetRuntimeAttributes();
            if (runtimeAttributes == null)
            {
                return;
            }

            foreach (Attribute attribute in runtimeAttributes.GetAllAttributes())
            {
                self.SyncRuntimeAttributeToNumeric(attribute);
            }
        }

        private static void BindRuntimeAttributeEvents(this AttributeComponent self)
        {
            AttributeSetContainer runtimeAttributes = self.GetRuntimeAttributes();
            if (runtimeAttributes == null)
            {
                return;
            }

            runtimeAttributes.OnAnyAttributeChanged += (attribute, _, _) =>
            {
                self.SyncRuntimeAttributeToNumeric(attribute);
            };
        }

        private static void AddRuntimeAttributeIfMissing(AttributeSetContainer runtimeAttributes, AttrType attrType, float value)
        {
            if (runtimeAttributes.GetAttribute(attrType) == null)
            {
                runtimeAttributes.AddAttribute(attrType, value);
            }
        }

        private static void RefreshRuntimeAttributeFromNumeric(
            this AttributeComponent self,
            AttributeSetContainer runtimeAttributes,
            NumericComponent numericComponent,
            AttrType attrType,
            bool overwriteExisting)
        {
            if (!TryGetNumericType(attrType, out int numericType) || !numericComponent.NumericDic.ContainsKey(numericType))
            {
                return;
            }

            float value = numericComponent.GetAsFloat(numericType);
            Attribute attribute = runtimeAttributes.GetAttribute(attrType);
            if (attribute == null)
            {
                runtimeAttributes.AddAttribute(attrType, value);
                return;
            }

            if (overwriteExisting)
            {
                attribute.Initialize(value);
            }
        }

        private static void SyncRuntimeAttributeToNumeric(this AttributeComponent self, Attribute attribute)
        {
            if (attribute == null || !TryGetNumericType(attribute.AttrType, out int numericType))
            {
                return;
            }

            NumericComponent numericComponent = self.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            numericComponent.Set(numericType, attribute.CurrentValue);
        }

        private static bool TryGetNumericType(AttrType attrType, out int numericType)
        {
            switch (attrType)
            {
                case AttrType.Health:
                    numericType = NumericType.Hp;
                    return true;
                case AttrType.MaxHealth:
                    numericType = NumericType.MaxHp;
                    return true;
                case AttrType.HealthRegen:
                    numericType = NumericType.HPRec;
                    return true;
                case AttrType.Mana:
                    numericType = NumericType.Mp;
                    return true;
                case AttrType.MaxMana:
                    numericType = NumericType.MaxMp;
                    return true;
                case AttrType.ManaRegen:
                    numericType = NumericType.MPRec;
                    return true;
                case AttrType.Attack:
                    numericType = NumericType.Attack;
                    return true;
                case AttrType.Defense:
                    numericType = NumericType.Armor;
                    return true;
                case AttrType.MagicPower:
                    numericType = NumericType.MagicStrength;
                    return true;
                case AttrType.MagicDefense:
                    numericType = NumericType.MagicResistance;
                    return true;
                case AttrType.MoveSpeed:
                    numericType = NumericType.Speed;
                    return true;
                case AttrType.AttackSpeed:
                    numericType = NumericType.AttackSpeed;
                    return true;
                case AttrType.CooldownReduction:
                    numericType = NumericType.SkillCD;
                    return true;
                case AttrType.CritRate:
                    numericType = NumericType.CriticalProbability;
                    return true;
                case AttrType.CritDamage:
                    numericType = NumericType.CriticalStrikeHarm;
                    return true;
                case AttrType.Level:
                    numericType = NumericType.Level;
                    return true;
                default:
                    numericType = 0;
                    return false;
            }
        }

        private static int GetValue(this AttributeComponent self, float v)
        {
            int ret = (int)v * 1000;
            return ret;
        }

        [EntitySystem]
        private static void Destroy(this AttributeComponent self)
        {
            self.GetRuntimeAttributes()?.Clear();
            self.RuntimeAttributes = null;
            self.AllModifiers.Dispose();
        }

        public static DataModifier AddModifier(this AttributeComponent self, int type, float value)
        {
            DataModifier modify = DataModifier.Create(self.DataId++, type, value);
            self.AllModifiers.Add(modify);
            self.CountAttr(modify.Attribute);
            return modify;
        }

        public static void RemoveModifer(this AttributeComponent self, DataModifier modify)
        {
            if (modify != null)
            {
                self.AllModifiers.Remove(modify);
                self.CountAttr(modify.Attribute);
                modify.Dispose();
            }
        }

        private static void CountAttr(this AttributeComponent self, int attributeType)
        {
            float value = 0;
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
