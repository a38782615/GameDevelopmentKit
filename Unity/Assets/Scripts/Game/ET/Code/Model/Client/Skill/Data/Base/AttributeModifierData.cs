using System;

namespace ET.Client
{
    [Serializable]
    public class AttributeModifierData : Object
    {
        public int attrType = global::ET.NumericType.None;
        public ModifierOperation operation = ModifierOperation.Add;
        public ModifierMagnitudeSourceType magnitudeSourceType = ModifierMagnitudeSourceType.FixedValue;
        public float fixedValue;
        public string formula = string.Empty;
        public MMCType mmcType = MMCType.AttributeBased;
        public string setByCallerKey = string.Empty;
        public int mmcCaptureAttribute = global::ET.NumericType.Attack;
        public MMCAttributeSource mmcAttributeSource = MMCAttributeSource.Source;
        public float mmcCoefficient = 1f;
        public bool mmcUseSnapshot = true;
    }

    public enum MMCAttributeSource
    {
        Source,
        Target
    }
}
