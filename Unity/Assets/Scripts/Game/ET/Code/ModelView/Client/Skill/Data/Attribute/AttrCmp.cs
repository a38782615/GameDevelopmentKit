using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 单个属性运行时实体。
    /// 作为 AttributeComponent 的子实体存在，基础值和当前值都落在 NumericComponent 上。
    /// </summary>
    [ChildOf(typeof(global::ET.AttributeComponent))]
    [EnableMethod]
    public class AttrCmp : Entity, IAwake<int>, IDestroy
    {
        // 直接使用 NumericType 作为属性标识，避免再维护一套 AttrType 映射。
        [SerializeField]
        private int numericType;
        public int NumericType => numericType;

        [SerializeField]
        private bool isMeta;
        public bool IsMeta => isMeta;

        [SerializeField]
        private bool hasMinValue;
        public bool HasMinValue => hasMinValue;

        [SerializeField]
        private float minValue;
        public float MinValue => minValue;

        [SerializeField]
        private bool hasMaxValue;
        public bool HasMaxValue => hasMaxValue;

        [SerializeField]
        private float maxValue;
        public float MaxValue => maxValue;

        // 运行时 modifier 只保存在内存中，不做持久化。
        [NonSerialized]
        private List<ActiveModifier> activeModifiers;

        [NonSerialized]
        private AggregatorMode aggregatorMode = AggregatorMode.Default;
        public AggregatorMode AggregatorMode
        {
            get => aggregatorMode;
            set => aggregatorMode = value;
        }

        [NonSerialized]
        private bool isDirty;

        public int ModifierCount => activeModifiers?.Count ?? 0;
        public float BaseValue
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                if (numericComponent == null)
                {
                    return 0f;
                }

                int baseNumericType = global::ET.NumericType.GetBaseNumericType(this.numericType);
                return baseNumericType == global::ET.NumericType.None
                    ? numericComponent.GetAsFloat(this.numericType)
                    : numericComponent.GetAsFloat(baseNumericType);
            }
            set
            {
                // BaseValue 变更后需要重新推导 CurrentValue。
                float oldValue = this.BaseValue;
                float newValue = ClampValue(value, true);

                WriteBaseValue(newValue, true);
                MarkDirty();
                Recalculate();
            }
        }

        public float CurrentValue
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                return numericComponent == null ? 0f : numericComponent.GetAsFloat(this.numericType);
            }
            set
            {
                float oldValue = this.CurrentValue;
                float newValue = ClampValue(value, false);

                WriteCurrentValue(newValue, true);
            }
        }

        public void Initialize(float value)
        {
            float newValue = ClampSilent(value);
            WriteBaseValue(newValue, false);
            WriteCurrentValue(newValue, false);
        }

        public void SetNumericType(int value)
        {
            this.numericType = value;
        }

        public void SetClamp(float? min, float? max)
        {
            hasMinValue = min.HasValue;
            if (min.HasValue)
            {
                minValue = min.Value;
            }

            hasMaxValue = max.HasValue;
            if (max.HasValue)
            {
                maxValue = max.Value;
            }
        }

        public void SetAsMeta(bool value = true)
        {
            isMeta = value;
        }

        public void ResetCurrentToBase()
        {
            CurrentValue = BaseValue;
        }

        public void AddModifier(AttributeModifier modifier, object source = null)
        {
            activeModifiers ??= new List<ActiveModifier>();
            activeModifiers.Add(new ActiveModifier
            {
                Modifier = modifier,
                Source = source,
                AppliedTime = Time.time
            });
            MarkDirty();
        }

        public bool RemoveModifier(AttributeModifier modifier)
        {
            if (activeModifiers == null)
            {
                return false;
            }

            int removed = activeModifiers.RemoveAll(m => m.Modifier == modifier);
            if (removed > 0)
            {
                MarkDirty();
                return true;
            }

            return false;
        }

        public int RemoveModifiersFromSource(object source)
        {
            if (activeModifiers == null)
            {
                return 0;
            }

            int removed = activeModifiers.RemoveAll(m => m.Source == source);
            if (removed > 0)
            {
                MarkDirty();
            }

            return removed;
        }

        public void ClearModifiers()
        {
            activeModifiers?.Clear();
            MarkDirty();
        }

        public void MarkDirty()
        {
            isDirty = true;
        }

        public void Recalculate(ModifierCalculationContext context = null)
        {
            if (!isDirty)
            {
                return;
            }

            float newValue = CalculateNewValue(context);
            CurrentValue = newValue;
            isDirty = false;
        }

        public IEnumerable<ActiveModifier> GetActiveModifiers()
        {
            return activeModifiers ?? (IEnumerable<ActiveModifier>)Array.Empty<ActiveModifier>();
        }

        public override string ToString()
        {
            return $"{ET.NumericType.GetAttributeName(this.numericType)}: Base={BaseValue}, Current={CurrentValue}";
        }

        private float CalculateNewValue(ModifierCalculationContext context)
        {
            if (activeModifiers == null || activeModifiers.Count == 0)
            {
                return BaseValue;
            }

            // 计算顺序与原 Attribute 实现保持一致：加法、乘法、覆盖。
            List<ActiveModifier> filteredModifiers = FilterModifiers(activeModifiers);
            float additive = 0f;
            float multiplicative = 1f;
            float? overrideValue = null;

            foreach (ActiveModifier activeModifier in filteredModifiers)
            {
                AttributeModifier modifier = activeModifier.Modifier;
                float magnitude = modifier.CalculateMagnitude(context);
                int stackCount = GetStackCount(activeModifier.Source);

                switch (modifier.Operation)
                {
                    case ModifierOperation.Add:
                        additive += magnitude * stackCount;
                        break;
                    case ModifierOperation.Multiply:
                        multiplicative *= 1f + (magnitude - 1f) * stackCount;
                        break;
                    case ModifierOperation.Divide:
                        if (Mathf.Abs(magnitude) > 0.0001f)
                        {
                            multiplicative /= 1f + (magnitude - 1f) * stackCount;
                        }
                        break;
                    case ModifierOperation.Override:
                        overrideValue = magnitude;
                        break;
                }
            }

            if (overrideValue.HasValue)
            {
                return overrideValue.Value;
            }

            return (BaseValue + additive) * multiplicative;
        }

        private List<ActiveModifier> FilterModifiers(List<ActiveModifier> modifiers)
        {
            switch (aggregatorMode)
            {
                case AggregatorMode.MostNegativeModifier:
                    return FilterMostNegative(modifiers);
                case AggregatorMode.MostPositiveModifier:
                    return FilterMostPositive(modifiers);
                case AggregatorMode.MostNegativeWithAllPositive:
                    return FilterMostNegativeWithAllPositive(modifiers);
                default:
                    return modifiers;
            }
        }

        private List<ActiveModifier> FilterMostNegative(List<ActiveModifier> modifiers)
        {
            List<ActiveModifier> result = new List<ActiveModifier>();
            ActiveModifier? mostNegative = null;
            float mostNegativeValue = 0f;

            foreach (ActiveModifier modifier in modifiers)
            {
                if (modifier.Modifier.Operation != ModifierOperation.Add)
                {
                    result.Add(modifier);
                    continue;
                }

                float magnitude = modifier.Modifier.CalculateMagnitude(null);
                if (magnitude < 0f && magnitude < mostNegativeValue)
                {
                    mostNegative = modifier;
                    mostNegativeValue = magnitude;
                }
            }

            if (mostNegative.HasValue)
            {
                result.Add(mostNegative.Value);
            }

            return result;
        }

        private List<ActiveModifier> FilterMostPositive(List<ActiveModifier> modifiers)
        {
            List<ActiveModifier> result = new List<ActiveModifier>();
            ActiveModifier? mostPositive = null;
            float mostPositiveValue = 0f;

            foreach (ActiveModifier modifier in modifiers)
            {
                if (modifier.Modifier.Operation != ModifierOperation.Add)
                {
                    result.Add(modifier);
                    continue;
                }

                float magnitude = modifier.Modifier.CalculateMagnitude(null);
                if (magnitude > 0f && magnitude > mostPositiveValue)
                {
                    mostPositive = modifier;
                    mostPositiveValue = magnitude;
                }
            }

            if (mostPositive.HasValue)
            {
                result.Add(mostPositive.Value);
            }

            return result;
        }

        private List<ActiveModifier> FilterMostNegativeWithAllPositive(List<ActiveModifier> modifiers)
        {
            List<ActiveModifier> result = new List<ActiveModifier>();
            ActiveModifier? mostNegative = null;
            float mostNegativeValue = 0f;

            foreach (ActiveModifier modifier in modifiers)
            {
                if (modifier.Modifier.Operation != ModifierOperation.Add)
                {
                    result.Add(modifier);
                    continue;
                }

                float magnitude = modifier.Modifier.CalculateMagnitude(null);
                if (magnitude >= 0f)
                {
                    result.Add(modifier);
                }
                else if (magnitude < mostNegativeValue)
                {
                    mostNegative = modifier;
                    mostNegativeValue = magnitude;
                }
            }

            if (mostNegative.HasValue)
            {
                result.Add(mostNegative.Value);
            }

            return result;
        }

        private float ClampValue(float value, bool isBaseValue)
        {
            float newValue = value;
            return ClampSilent(newValue);
        }

        private float ClampSilent(float value)
        {
            float result = value;
            if (hasMinValue)
            {
                result = Mathf.Max(result, minValue);
            }

            if (hasMaxValue)
            {
                result = Mathf.Min(result, maxValue);
            }

            return result;
        }

        private void WriteBaseValue(float value, bool publishEvent)
        {
            NumericComponent numericComponent = GetNumericComponent();
            if (numericComponent == null)
            {
                return;
            }

            int baseNumericType = global::ET.NumericType.GetBaseNumericType(this.numericType);
            if (baseNumericType == global::ET.NumericType.None)
            {
                WriteCurrentValue(value, publishEvent);
                return;
            }

            if (publishEvent)
            {
                numericComponent.Set(baseNumericType, value);
            }
            else
            {
                numericComponent.SetNoEvent(baseNumericType, (long)(value * 10000));
                numericComponent.Update(baseNumericType, false);
            }
        }

        private void WriteCurrentValue(float value, bool publishEvent)
        {
            NumericComponent numericComponent = GetNumericComponent();
            if (numericComponent == null)
            {
                return;
            }

            if (publishEvent)
            {
                numericComponent.Set(this.numericType, value);
            }
            else
            {
                numericComponent.SetNoEvent(this.numericType, (long)(value * 10000));
            }
        }

        private NumericComponent GetNumericComponent()
        {
            return this.GetParent<global::ET.AttributeComponent>()?.NumericComponent;
        }

        private static int GetStackCount(object source)
        {
            if (source == null)
            {
                return 1;
            }

            var stackCountProperty = source.GetType().GetProperty("StackCount");
            if (stackCountProperty == null)
            {
                return 1;
            }

            object value = stackCountProperty.GetValue(source);
            return value is int stackCount ? stackCount : 1;
        }
    }

    public struct ActiveModifier
    {
        public AttributeModifier Modifier;
        public object Source;
        public float AppliedTime;
    }

    public enum AggregatorMode
    {
        Default,
        MostNegativeModifier,
        MostPositiveModifier,
        MostNegativeWithAllPositive
    }
}
