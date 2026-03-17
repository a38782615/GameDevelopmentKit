namespace ET.Client
{
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(global::ET.AttributeComponent))]
    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this global::ET.AttributeComponent self)
        {
            self.AllModifiers = XList<DataModifier>.Create();
            self.RefreshRuntimeAttributesFromNumeric(false);
        }

        [EntitySystem]
        private static void Destroy(this global::ET.AttributeComponent self)
        {
            self.Clear();
            self.AllModifiers.Dispose();
        }

        public static void Init(this global::ET.AttributeComponent self, bool isHero)
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
            num.SetNoEvent(global::ET.NumericType.MaxHpBase, GetConfigValue(unitBaseConfig.HP));
            num.SetNoEvent(global::ET.NumericType.HpBase, GetConfigValue(unitBaseConfig.HP));
            num.SetNoEvent(global::ET.NumericType.CriticalProbabilityBase, GetConfigValue(unitBaseConfig.CriticalProbability));
            num.SetNoEvent(global::ET.NumericType.ModeBase, GetConfigValue(unitBaseConfig.Mode));
            num.SetNoEvent(global::ET.NumericType.ModeMaxBase, GetConfigValue(unitBaseConfig.Mode));
            num.SetNoEvent(global::ET.NumericType.MpBase, GetConfigValue(unitBaseConfig.MP));
            num.SetNoEvent(global::ET.NumericType.MaxMpBase, GetConfigValue(unitBaseConfig.MP));
            num.SetNoEvent(global::ET.NumericType.AttackBase, GetConfigValue(unitBaseConfig.Attack));
            num.SetNoEvent(global::ET.NumericType.ArmorBase, GetConfigValue(unitBaseConfig.Armor));
            num.SetNoEvent(global::ET.NumericType.SpeedBase, unitBaseConfig.MoveSpeed);
            num.SetNoEvent(global::ET.NumericType.AttackSpeedBase, GetConfigValue(unitBaseConfig.AttackSpeed));
            self.RefreshRuntimeAttributesFromNumeric(true);
        }

        public static AttrCmp AddAttribute(this global::ET.AttributeComponent self, int numericType, float defaultValue = 0f)
        {
            AttrCmp existing = self.GetAttrCmp(numericType);
            if (existing != null)
            {
                return existing;
            }

            EnsureNumericValue(self, numericType, defaultValue);

            AttrCmp attribute = self.AddChildWithId<AttrCmp, int>(numericType, numericType);
            attribute.Initialize(self.NumericComponent?.GetAsFloat(numericType) ?? defaultValue);
            BindAttributeEvents(self, attribute);
            return attribute;
        }

        public static AttrCmp AddMetaAttribute(this global::ET.AttributeComponent self, int numericType)
        {
            AttrCmp attribute = self.AddAttribute(numericType, 0f);
            attribute.SetAsMeta(true);
            return attribute;
        }

        public static void RefreshRuntimeAttributesFromNumeric(this global::ET.AttributeComponent self, bool overwriteExisting)
        {
            foreach (int numericType in global::ET.NumericType.GetClientAttributeTypes())
            {
                RefreshRuntimeAttributeFromNumeric(self, numericType, overwriteExisting);
            }
        }

        public static void InitializeMissingRuntimeAttributesFromConfig(this global::ET.AttributeComponent self)
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

            AddAttributeIfMissing(self, global::ET.NumericType.Hp, unitBaseConfig.HP);
            AddAttributeIfMissing(self, global::ET.NumericType.MaxHp, unitBaseConfig.HP);
            AddAttributeIfMissing(self, global::ET.NumericType.Mp, unitBaseConfig.MP);
            AddAttributeIfMissing(self, global::ET.NumericType.MaxMp, unitBaseConfig.MP);
            AddAttributeIfMissing(self, global::ET.NumericType.Attack, unitBaseConfig.Attack);
            AddAttributeIfMissing(self, global::ET.NumericType.Armor, unitBaseConfig.Armor);
            AddAttributeIfMissing(self, global::ET.NumericType.Speed, unitBaseConfig.MoveSpeed / 10000f);
            AddAttributeIfMissing(self, global::ET.NumericType.AttackSpeed, unitBaseConfig.AttackSpeed);
            AddAttributeIfMissing(self, global::ET.NumericType.CriticalProbability, unitBaseConfig.CriticalProbability);
            AddAttributeIfMissing(self, global::ET.NumericType.Level, level);
            self.SyncAllRuntimeAttributesToNumeric();
        }

        public static void SyncAllRuntimeAttributesToNumeric(this global::ET.AttributeComponent self)
        {
            foreach (AttrCmp attribute in self.GetAllAttributes())
            {
                SyncRuntimeAttributeToNumeric(self, attribute);
            }
        }

        public static DataModifier AddModifier(this global::ET.AttributeComponent self, int type, float value)
        {
            DataModifier modify = DataModifier.Create(self.DataId++, type, value);
            self.AllModifiers.Add(modify);
            CountAttr(self, modify.Attribute);
            return modify;
        }

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

        private static void AddAttributeIfMissing(this global::ET.AttributeComponent self, int numericType, float value)
        {
            if (!self.HasAttribute(numericType))
            {
                self.AddAttribute(numericType, value);
            }
        }

        private static void BindAttributeEvents(global::ET.AttributeComponent self, AttrCmp attribute)
        {
            attribute.OnPostBaseValueChange += (attr, oldValue, newValue) =>
            {
                SyncRuntimeAttributeToNumeric(self, attr);
                PublishAttributeChanged(self, attr, oldValue, newValue);
            };

            attribute.OnPostCurrentValueChange += (attr, oldValue, newValue) =>
            {
                SyncRuntimeAttributeToNumeric(self, attr);
                PublishAttributeChanged(self, attr, oldValue, newValue);
            };
        }

        private static void PublishAttributeChanged(global::ET.AttributeComponent self, AttrCmp attribute, float oldValue, float newValue)
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
                NumericType = attribute.NumericType,
                OldValue = oldValue,
                NewValue = newValue
            });
        }

        private static void RefreshRuntimeAttributeFromNumeric(global::ET.AttributeComponent self, int numericType, bool overwriteExisting)
        {
            float value = self.NumericComponent?.GetAsFloat(numericType) ?? 0f;
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                self.AddAttribute(numericType, value);
                return;
            }

            if (overwriteExisting)
            {
                attribute.Initialize(value);
            }
        }

        private static void SyncRuntimeAttributeToNumeric(global::ET.AttributeComponent self, AttrCmp attribute)
        {
            if (attribute == null || self.NumericComponent == null)
            {
                return;
            }

            self.NumericComponent.Set(attribute.NumericType, attribute.CurrentValue);
        }

        private static void EnsureNumericValue(global::ET.AttributeComponent self, int numericType, float defaultValue)
        {
            NumericComponent numericComponent = self.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            int baseNumericType = global::ET.NumericType.GetBaseNumericType(numericType);
            if (baseNumericType != global::ET.NumericType.None && numericComponent.GetByKey(baseNumericType) == 0)
            {
                numericComponent.SetNoEvent(baseNumericType, (long)(defaultValue * 10000));
                numericComponent.Update(baseNumericType, false);
                return;
            }

            if (numericComponent.GetByKey(numericType) == 0)
            {
                numericComponent.SetNoEvent(numericType, (long)(defaultValue * 10000));
            }
        }

        private static int GetConfigValue(float value)
        {
            return (int)value * 1000;
        }

        private static void CountAttr(global::ET.AttributeComponent self, int attributeType)
        {
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
