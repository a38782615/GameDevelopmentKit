namespace ET.Client
{
    /// <summary>
    /// AttributeComponent 的运行时逻辑。
    /// 负责按 NumericComponent 初始化/同步 AttrCmp 子实体，并派发属性变化事件。
    /// </summary>
    [EntitySystemOf(typeof(AttributeComponent))]
    [FriendOfAttribute(typeof(global::ET.AttributeComponent))]
    [FriendOfAttribute(typeof(global::ET.NumericComponent))]
    public static partial class AttributeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this global::ET.AttributeComponent self)
        {
            self.AllModifiers = XList<DataModifier>.Create();
        }

        [EntitySystem]
        private static void Destroy(this global::ET.AttributeComponent self)
        {
            self.Clear();
            self.AllModifiers?.Dispose();
        }

        public static void Init(this global::ET.AttributeComponent self)
        {
            NumericComponent numericComponent = self.NumericComponent;
            if (numericComponent == null)
            {
                return;
            }

            if (TryGetUnitBaseConfig(self, out Unit unit, out DRUnitAttribute unitBaseConfig))
            {
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.MaxHp, ToNumericLong(unitBaseConfig.HP));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Hp, ToNumericLong(unitBaseConfig.HP));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.CriticalProbability, ToNumericLong(unitBaseConfig.CriticalProbability));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Mode, ToNumericLong(unitBaseConfig.Mode));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.ModeMax, ToNumericLong(unitBaseConfig.Mode));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Mp, ToNumericLong(unitBaseConfig.MP));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.MaxMp, ToNumericLong(unitBaseConfig.MP));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Attack, ToNumericLong(unitBaseConfig.Attack));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Armor, ToNumericLong(unitBaseConfig.Armor));
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.Speed, unitBaseConfig.MoveSpeed);
                TryApplyNumericFromConfig(numericComponent, global::ET.NumericType.AttackSpeed, ToNumericLong(unitBaseConfig.AttackSpeed));
            }

            self.RefreshRuntimeAttributesFromNumeric(true);
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
            attribute.Initialize(self.NumericComponent?.GetAsFloat(numericType) ?? defaultValue);
            return attribute;
        }

        public static void RefreshRuntimeAttributesFromNumeric(this global::ET.AttributeComponent self, bool overwriteExisting)
        {
            // 统一从 NumericComponent 拉取客户端关心的属性，构建或刷新 AttrCmp。
            foreach (int numericType in global::ET.NumericType.GetClientAttributeTypes())
            {
                RefreshRuntimeAttributeFromNumeric(self, numericType, overwriteExisting);
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
        private static bool TryGetUnitBaseConfig(
            global::ET.AttributeComponent self,
            out Unit unit,
            out DRUnitAttribute unitBaseConfig)
        {
            unit = self.GetParent<Unit>();
            unitBaseConfig = null;
            if (unit == null)
            {
                return false;
            }

            PlayerData playerData = self.Root()?.GetComponent<GameDataMgrComponent>()?.GetPlayerDataComponent()?.PlayerData;
            unitBaseConfig = Tables.Instance.DTUnitAttribute.Get(unit.ConfigId, playerData.Level, playerData.SubLevel);
            return unitBaseConfig != null;
        }

        private static void TryApplyNumericFromConfig(NumericComponent numericComponent, int numericType, long value)
        {
            int baseNumericType = global::ET.NumericType.GetBaseNumericType(numericType);
            if (baseNumericType != global::ET.NumericType.None)
            {
                if (!numericComponent.NumericDic.ContainsKey(baseNumericType))
                {
                    numericComponent.SetNoEvent(baseNumericType, value);
                }

                if (!numericComponent.NumericDic.ContainsKey(numericType))
                {
                    numericComponent.Update(baseNumericType, false);
                }

                return;
            }

            if (!numericComponent.NumericDic.ContainsKey(numericType))
            {
                numericComponent.SetNoEvent(numericType, value);
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
