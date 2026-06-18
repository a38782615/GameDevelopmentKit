using System;
using System.Collections.Generic;

namespace ET.Client
{
    [Serializable]
    public class AttributeModifier : Object
    {
        private int targetAttrType;
        private ModifierOperation operation = ModifierOperation.Add;
        private float magnitude;
        private string magnitudeFormula;
        private MMCType mmcType = MMCType.AttributeBased;
        private bool useMMC;
        private AttributeCaptureType captureType = AttributeCaptureType.Snapshot;
        private int mmcCaptureAttribute = global::ET.NumericType.Attack;
        private MMCAttributeSource mmcAttributeSource = MMCAttributeSource.Source;
        private float mmcCoefficient = 1f;
        private bool mmcUseSnapshot = true;

        public int TargetAttrType
        {
            get => targetAttrType;
            set => targetAttrType = value;
        }

        public ModifierOperation Operation
        {
            get => operation;
            set => operation = value;
        }

        public float Magnitude
        {
            get => magnitude;
            set => magnitude = value;
        }

        public string MagnitudeFormula
        {
            get => magnitudeFormula;
            set => magnitudeFormula = value;
        }

        public MMCType MMCType
        {
            get => mmcType;
            set => mmcType = value;
        }

        public bool UseMMC
        {
            get => useMMC;
            set => useMMC = value;
        }

        public AttributeCaptureType CaptureType
        {
            get => captureType;
            set => captureType = value;
        }

        public int MMCCaptureAttribute
        {
            get => mmcCaptureAttribute;
            set => mmcCaptureAttribute = value;
        }

        public MMCAttributeSource MMCAttributeSource
        {
            get => mmcAttributeSource;
            set => mmcAttributeSource = value;
        }

        public float MMCCoefficient
        {
            get => mmcCoefficient;
            set => mmcCoefficient = value;
        }

        public bool MMCUseSnapshot
        {
            get => mmcUseSnapshot;
            set => mmcUseSnapshot = value;
        }

        public AttributeModifier()
        {
        }

        public AttributeModifier(int targetAttrType, ModifierOperation operation, float magnitude)
        {
            this.targetAttrType = targetAttrType;
            this.operation = operation;
            this.magnitude = magnitude;
        }

        public float CalculateMagnitude(ModifierCalculationContext context)
        {
            if (useMMC)
            {
                return CalculateMMC(context);
            }

            if (!string.IsNullOrEmpty(magnitudeFormula))
            {
                return EvaluateFormula(context);
            }

            return magnitude;
        }

        public static AttributeModifier FromData(AttributeModifierData data)
        {
            var modifier = new AttributeModifier
            {
                targetAttrType = data.attrType,
                operation = data.operation,
            };

            switch (data.magnitudeSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    modifier.magnitude = data.fixedValue;
                    break;
                case ModifierMagnitudeSourceType.Formula:
                    modifier.magnitudeFormula = data.formula;
                    break;
                case ModifierMagnitudeSourceType.ModifierMagnitudeCalculation:
                    modifier.useMMC = true;
                    modifier.mmcType = data.mmcType;
                    modifier.mmcCaptureAttribute = data.mmcCaptureAttribute;
                    modifier.mmcAttributeSource = data.mmcAttributeSource;
                    modifier.mmcCoefficient = data.mmcCoefficient;
                    modifier.mmcUseSnapshot = data.mmcUseSnapshot;
                    break;
                case ModifierMagnitudeSourceType.SetByCaller:
                    break;
            }

            return modifier;
        }

        public AttributeModifierData ToData()
        {
            var data = new AttributeModifierData
            {
                attrType = targetAttrType,
                operation = operation,
            };

            if (useMMC)
            {
                data.magnitudeSourceType = ModifierMagnitudeSourceType.ModifierMagnitudeCalculation;
                data.mmcType = mmcType;
            }
            else if (!string.IsNullOrEmpty(magnitudeFormula))
            {
                data.magnitudeSourceType = ModifierMagnitudeSourceType.Formula;
                data.formula = magnitudeFormula;
            }
            else
            {
                data.magnitudeSourceType = ModifierMagnitudeSourceType.FixedValue;
                data.fixedValue = magnitude;
            }

            return data;
        }

        public override string ToString()
        {
            string opStr = operation switch
            {
                ModifierOperation.Add => "+",
                ModifierOperation.Multiply => "*",
                ModifierOperation.Divide => "/",
                ModifierOperation.Override => "=",
                _ => "?"
            };

            string valueStr = !string.IsNullOrEmpty(magnitudeFormula) ? magnitudeFormula : magnitude.ToString();
            return $"{targetAttrType} {opStr} {valueStr}";
        }

        private float CalculateMMC(ModifierCalculationContext context)
        {
            if (mmcType == MMCType.AttributeBased)
            {
                float? attrValue = null;

                if (mmcUseSnapshot && context?.SnapshotValues != null)
                {
                    attrValue = context.GetSnapshotValue(mmcCaptureAttribute);
                }

                if (!attrValue.HasValue)
                {
                    attrValue = mmcAttributeSource == MMCAttributeSource.Source
                        ? context?.GetSourceAttribute(mmcCaptureAttribute)
                        : context?.GetTargetAttribute(mmcCaptureAttribute);
                }

                return (attrValue ?? 0f) * mmcCoefficient;
            }

            if (mmcType == MMCType.LevelBased)
            {
                int level = context?.EffectLevel ?? 1;
                return magnitude * (1f + level * 0.1f);
            }

            return magnitude;
        }

        private float EvaluateFormula(ModifierCalculationContext context)
        {
            return context == null ? magnitude : magnitude;
        }
    }

    public enum AttributeCaptureType
    {
        Snapshot,
        Track
    }

    public class ModifierCalculationContext : Object
    {
        public global::ET.AttributeComponent SourceAttributes { get; set; }
        public global::ET.AttributeComponent TargetAttributes { get; set; }
        public Dictionary<int, float> SnapshotValues { get; set; }
        public Dictionary<int, float> SourceAttributeOverrides { get; set; }
        public int EffectLevel { get; set; } = 1;
        public Dictionary<string, object> CustomData { get; set; }

        public float? GetSourceAttribute(int attrType)
        {
            if (SourceAttributeOverrides != null && SourceAttributeOverrides.TryGetValue(attrType, out float overrideValue))
            {
                return overrideValue;
            }

            return GetValue(SourceAttributes, attrType);
        }

        public float? GetTargetAttribute(int attrType)
        {
            return GetValue(TargetAttributes, attrType);
        }

        public float? GetSnapshotValue(int attrType)
        {
            if (SnapshotValues != null && SnapshotValues.TryGetValue(attrType, out float value))
            {
                return value;
            }

            return null;
        }

        public void SetCustomData(string key, object value)
        {
            CustomData ??= new Dictionary<string, object>();
            CustomData[key] = value;
        }

        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData != null && CustomData.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        public AttrCmp GetAttrCmp(AttributeComponent self, int numericType)
        {
            return self?.GetChild<AttrCmp>(numericType);
        }
        
        public float GetValue(AttributeComponent self, int numericType)
        {
            AttrCmp attribute = GetAttrCmp(self, numericType);
            return attribute.ValueFloat;
        }
    }
}
