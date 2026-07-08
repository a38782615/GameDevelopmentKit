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

        [NonSerialized]
        private List<ActiveModifier> activeModifiers = new List<ActiveModifier>();

        [NonSerialized]
        private bool isDirty;
        public int NumericType;
        public int ModifierCount => activeModifiers.Count;

        public void SetBaseValue(float value)
        {
            GetNumericComponent().Set(NumericType * 10 + 1, value);
        }

        public float ValueFloat
        {
            get
            {
                return GetNumericComponent().GetAsFloat(NumericType);
            }
        }

        public long ValueLong
        {
            get
            {
                return GetNumericComponent().GetAsLong(NumericType);
            }
        }

        public void AddModifier(AttributeModifier modifier, object source = null, int stackCount = 1)
        {
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
            SetBaseValue(newValue);
            isDirty = false;
        }

        public override string ToString()
        {
            return $"{ET.NumericType.GetAttributeName(NumericType)}: Value={ValueFloat}";
        }

        private float CalculateNewValue(ModifierCalculationContext context)
        {
            if (activeModifiers == null || activeModifiers.Count == 0)
            {
                return ValueFloat;
            }

            float additive = 0f;
            float multiplicative = 1f;
            float? overrideValue = null;

            foreach (ActiveModifier activeModifier in activeModifiers)
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

            return (ValueFloat + additive) * multiplicative;
        }

        private NumericComponent GetNumericComponent()
        {
            return GetParent<global::ET.AttributeComponent>().NumericComponent;
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
