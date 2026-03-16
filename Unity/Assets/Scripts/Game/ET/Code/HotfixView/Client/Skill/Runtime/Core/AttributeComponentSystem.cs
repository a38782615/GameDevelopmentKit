using System.Collections.Generic;

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
            EnsureAttributeMap(self);
            self.RefreshRuntimeAttributesFromNumeric(false);
        }

        [EntitySystem]
        private static void Destroy(this AttributeComponent self)
        {
            self.Clear();
            self.RuntimeAttributes = null;
            self.AllModifiers.Dispose();
        }

        public static void Init(this AttributeComponent self, bool isHero)
        {
            Unit unit = self.GetParent<Unit>();
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

        public static Attribute AddAttribute(this AttributeComponent self, AttrType attrType, float defaultValue = 0f)
        {
            Dictionary<AttrType, Attribute> attributes = EnsureAttributeMap(self);
            if (attributes.TryGetValue(attrType, out Attribute existing))
            {
                Log.Warning($"Attribute '{attrType}' already exists");
                return existing;
            }

            Attribute attribute = new Attribute(attrType, defaultValue);
            attributes[attrType] = attribute;
            self.BindAttributeEvents(attribute);
            return attribute;
        }

        public static Attribute AddMetaAttribute(this AttributeComponent self, AttrType attrType)
        {
            Attribute attribute = self.AddAttribute(attrType, 0f);
            attribute.SetAsMeta(true);
            return attribute;
        }

        public static void RefreshRuntimeAttributesFromNumeric(this AttributeComponent self, bool overwriteExisting)
        {
            self.RefreshRuntimeAttributeFromNumeric(AttrType.Health, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.MaxHealth, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.HealthRegen, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.Mana, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.MaxMana, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.ManaRegen, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.Attack, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.Defense, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.MagicPower, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.MagicDefense, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.MoveSpeed, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.AttackSpeed, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.CooldownReduction, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.CritRate, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.CritDamage, overwriteExisting);
            self.RefreshRuntimeAttributeFromNumeric(AttrType.Level, overwriteExisting);
        }

        public static void InitializeMissingRuntimeAttributesFromConfig(this AttributeComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null)
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

            self.AddAttributeIfMissing(AttrType.Health, unitBaseConfig.HP);
            self.AddAttributeIfMissing(AttrType.MaxHealth, unitBaseConfig.HP);
            self.AddAttributeIfMissing(AttrType.Mana, unitBaseConfig.MP);
            self.AddAttributeIfMissing(AttrType.MaxMana, unitBaseConfig.MP);
            self.AddAttributeIfMissing(AttrType.Attack, unitBaseConfig.Attack);
            self.AddAttributeIfMissing(AttrType.Defense, unitBaseConfig.Armor);
            self.AddAttributeIfMissing(AttrType.MoveSpeed, unitBaseConfig.MoveSpeed / 10000f);
            self.AddAttributeIfMissing(AttrType.AttackSpeed, unitBaseConfig.AttackSpeed);
            self.AddAttributeIfMissing(AttrType.CritRate, unitBaseConfig.CriticalProbability);
            self.AddAttributeIfMissing(AttrType.Level, level);
            self.SyncAllRuntimeAttributesToNumeric();
        }

        public static void SyncAllRuntimeAttributesToNumeric(this AttributeComponent self)
        {
            foreach (Attribute attribute in self.GetAllAttributes())
            {
                self.SyncRuntimeAttributeToNumeric(attribute);
            }
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

        private static void AddAttributeIfMissing(this AttributeComponent self, AttrType attrType, float value)
        {
            if (!self.HasAttribute(attrType))
            {
                self.AddAttribute(attrType, value);
            }
        }

        private static Dictionary<AttrType, Attribute> EnsureAttributeMap(this AttributeComponent self)
        {
            if (self.RuntimeAttributes is not Dictionary<AttrType, Attribute> attributes)
            {
                attributes = new Dictionary<AttrType, Attribute>();
                self.RuntimeAttributes = attributes;
            }

            return attributes;
        }

        private static void BindAttributeEvents(this AttributeComponent self, Attribute attribute)
        {
            attribute.OnPostBaseValueChange += (attr, oldValue, newValue) =>
            {
                self.SyncRuntimeAttributeToNumeric(attr);
                self.PublishAttributeChanged(attr, oldValue, newValue);
            };

            attribute.OnPostCurrentValueChange += (attr, oldValue, newValue) =>
            {
                self.SyncRuntimeAttributeToNumeric(attr);
                self.PublishAttributeChanged(attr, oldValue, newValue);
            };
        }

        private static void PublishAttributeChanged(this AttributeComponent self, Attribute attribute, float oldValue, float newValue)
        {
            Unit unit = self.GetParent<Unit>();
            Scene scene = self.Scene();
            if (unit == null || scene == null)
            {
                return;
            }

            EventSystem.Instance.Publish(scene, new AttributeValueChanged
            {
                Unit = unit,
                AttrType = (int)attribute.AttrType,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        private static void RefreshRuntimeAttributeFromNumeric(this AttributeComponent self, AttrType attrType, bool overwriteExisting)
        {
            if (!TryGetNumericType(attrType, out int numericType))
            {
                return;
            }

            float value = self.NumericComponent?.GetAsFloat(numericType) ?? 0f;
            Attribute attribute = self.GetAttribute(attrType);
            if (attribute == null)
            {
                self.AddAttribute(attrType, value);
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
            return (int)v * 1000;
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
