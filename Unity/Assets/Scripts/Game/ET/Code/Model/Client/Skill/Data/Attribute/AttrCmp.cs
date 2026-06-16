using System;
using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [ChildOf(typeof(global::ET.AttributeComponent))]
    [EnableMethod]
    public class AttrCmp : Entity, IAwake<int>, IDestroy
    {
        private const float Epsilon = 0.0001f;

        private int numericType;
        private bool isMeta;
        private bool hasMinValue;
        private float minValue;
        private bool hasMaxValue;
        private float maxValue;

        [NonSerialized]
        private List<ActiveModifier> activeModifiers;

        [NonSerialized]
        private AggregatorMode aggregatorMode = AggregatorMode.Default;

        [NonSerialized]
        private bool isDirty;

        public int NumericType => numericType;
        public bool IsMeta => isMeta;
        public bool HasMinValue => hasMinValue;
        public float MinValue => minValue;
        public bool HasMaxValue => hasMaxValue;
        public float MaxValue => maxValue;
        public int ModifierCount => activeModifiers?.Count ?? 0;

        public AggregatorMode AggregatorMode
        {
            get => aggregatorMode;
            set => aggregatorMode = value;
        }

        public float BaseValue
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                if (numericComponent == null)
                {
                    return 0f;
                }

                int baseNumericType = global::ET.NumericType.GetBaseNumericType(numericType);
                return baseNumericType == global::ET.NumericType.None
                    ? numericComponent.GetAsFloat(numericType)
                    : numericComponent.GetAsFloat(baseNumericType);
            }
            set
            {
                float newValue = ClampSilent(value);
                WriteBaseValue(newValue, true);
                MarkDirty();
                Recalculate();
            }
        }

        public int BaseValueInt
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                if (numericComponent == null)
                {
                    return 0;
                }

                int baseNumericType = global::ET.NumericType.GetBaseNumericType(numericType);
                int targetNumericType = baseNumericType == global::ET.NumericType.None ? numericType : baseNumericType;
                return TruncateRawNumericToInt(numericComponent.GetAsInt(targetNumericType));
            }
        }

        public float CurrentValue
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                return numericComponent == null ? 0f : numericComponent.GetAsFloat(numericType);
            }
            set
            {
                float newValue = ClampSilent(value);
                WriteCurrentValue(newValue, true);
                ClampDependentAttributes();
            }
        }

        public int CurrentValueInt
        {
            get
            {
                NumericComponent numericComponent = GetNumericComponent();
                return numericComponent == null ? 0 : TruncateRawNumericToInt(numericComponent.GetAsInt(numericType));
            }
        }

        public void Initialize(float value)
        {
            float newValue = ClampSilent(value);
            WriteBaseValue(newValue, false);
            WriteCurrentValue(newValue, false);
            ClampDependentAttributes();
        }

        public void SetNumericType(int value)
        {
            numericType = value;
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

        public void AddModifier(AttributeModifier modifier, object source = null, int stackCount = 1)
        {
            activeModifiers ??= new List<ActiveModifier>();
            activeModifiers.Add(new ActiveModifier
            {
                Modifier = modifier,
                Source = source,
                SourceProperties = new ModifierSourceProperties
                {
                    StackCount = math.max(1, stackCount),
                },
                AppliedTime = global::ET.TimeInfo.Instance.ClientNow(),
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
            if (removed <= 0)
            {
                return false;
            }

            MarkDirty();
            return true;
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

        public void UpdateModifierSourceProperties(object source, int stackCount)
        {
            if (activeModifiers == null)
            {
                return;
            }

            int normalizedStackCount = math.max(1, stackCount);
            for (int index = 0; index < activeModifiers.Count; ++index)
            {
                ActiveModifier activeModifier = activeModifiers[index];
                if (!ReferenceEquals(activeModifier.Source, source))
                {
                    continue;
                }

                activeModifier.SourceProperties.StackCount = normalizedStackCount;
                activeModifiers[index] = activeModifier;
            }
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
            return $"{ET.NumericType.GetAttributeName(numericType)}: Base={BaseValue}, Current={CurrentValue}";
        }

        private float CalculateNewValue(ModifierCalculationContext context)
        {
            if (activeModifiers == null || activeModifiers.Count == 0)
            {
                return BaseValue;
            }

            List<ActiveModifier> filteredModifiers = FilterModifiers(activeModifiers);
            float additive = 0f;
            float multiplicative = 1f;
            float? overrideValue = null;

            foreach (ActiveModifier activeModifier in filteredModifiers)
            {
                AttributeModifier modifier = activeModifier.Modifier;
                float magnitude = modifier.CalculateMagnitude(context);
                int stackCount = activeModifier.SourceProperties.StackCount;

                switch (modifier.Operation)
                {
                    case ModifierOperation.Add:
                        additive += magnitude * stackCount;
                        break;
                    case ModifierOperation.Multiply:
                        multiplicative *= 1f + (magnitude - 1f) * stackCount;
                        break;
                    case ModifierOperation.Divide:
                        if (math.abs(magnitude) > Epsilon)
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
            return aggregatorMode switch
            {
                AggregatorMode.MostNegativeModifier => FilterMostNegative(modifiers),
                AggregatorMode.MostPositiveModifier => FilterMostPositive(modifiers),
                AggregatorMode.MostNegativeWithAllPositive => FilterMostNegativeWithAllPositive(modifiers),
                _ => modifiers,
            };
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

        private float ClampSilent(float value)
        {
            float result = value;
            if (numericType == global::ET.NumericType.Hp)
            {
                NumericComponent numericComponent = GetNumericComponent();
                float maxHealth = numericComponent?.GetAsFloat(global::ET.NumericType.MaxHp) ?? 0f;
                if (maxHealth > 0f)
                {
                    result = math.min(result, maxHealth);
                }
            }

            if (hasMinValue)
            {
                result = math.max(result, minValue);
            }

            if (hasMaxValue)
            {
                result = math.min(result, maxValue);
            }

            return result;
        }

        private void ClampDependentAttributes()
        {
            if (numericType != global::ET.NumericType.MaxHp)
            {
                return;
            }

            global::ET.AttributeComponent attributeComponent = GetParent<global::ET.AttributeComponent>();
            AttrCmp healthAttribute = attributeComponent?.GetAttrCmp(global::ET.NumericType.Hp);
            if (healthAttribute == null)
            {
                return;
            }

            float maxHealth = maxValue;
            if (maxHealth <= 0f)
            {
                return;
            }

            if (healthAttribute.BaseValue > maxHealth + Epsilon)
            {
                healthAttribute.BaseValue = maxHealth;
                return;
            }

            if (healthAttribute.CurrentValue > maxHealth + Epsilon)
            {
                healthAttribute.CurrentValue = maxHealth;
            }
        }

        private void WriteBaseValue(float value, bool publishEvent)
        {
            NumericComponent numericComponent = GetNumericComponent();
            if (numericComponent == null)
            {
                return;
            }

            int baseNumericType = global::ET.NumericType.GetBaseNumericType(numericType);
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
                numericComponent.Set(numericType, value);
            }
            else
            {
                numericComponent.SetNoEvent(numericType, (long)(value * 10000));
            }
        }

        private NumericComponent GetNumericComponent()
        {
            return GetParent<global::ET.AttributeComponent>()?.NumericComponent;
        }

        private static int TruncateRawNumericToInt(int value)
        {
            return value / 10000;
        }
    }

    public struct ActiveModifier
    {
        public AttributeModifier Modifier;
        public object Source;
        public ModifierSourceProperties SourceProperties;
        public long AppliedTime;
    }

    public struct ModifierSourceProperties
    {
        public int StackCount;
    }

    public enum AggregatorMode
    {
        Default,
        MostNegativeModifier,
        MostPositiveModifier,
        MostNegativeWithAllPositive
    }
}
