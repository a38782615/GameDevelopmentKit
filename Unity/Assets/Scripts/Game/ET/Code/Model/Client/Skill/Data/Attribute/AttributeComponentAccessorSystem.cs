using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [FriendOfAttribute(typeof(global::ET.AttributeComponent))]
    [FriendOfAttribute(typeof(ET.Client.AttrCmp))]
    public static class AttributeComponentAccessorSystem
    {

        public static AttrCmp GetAttrCmp(this global::ET.AttributeComponent self, int numericType)
        {
            return self?.GetChild<AttrCmp>(numericType);
        }

        public static AttrCmp GetAttribute(this global::ET.AttributeComponent self, int numericType)
        {
            return self.GetAttrCmp(numericType);
        }

        public static bool RemoveAttribute(this global::ET.AttributeComponent self, int numericType)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                return false;
            }

            attribute.ClearModifiers();
            attribute.Dispose();
            return true;
        }

        public static float GetValue(this global::ET.AttributeComponent self, int numericType)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            return attribute.ValueFloat;
        }

        public static bool SetValue(this global::ET.AttributeComponent self, int numericType, float value)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                return false;
            }

            var max = self.GetMax(numericType);
            var maxV = value;
            if (max > -1)
            {
                maxV = self.GetAttrCmp(max).ValueFloat;
            }
            var v = math.clamp(value, 0, maxV);
            attribute.SetBaseValue(v);
            return true;
        }

        private static int GetMax(this global::ET.AttributeComponent self, int numericType)
        {
            if (numericType == NumericType.Hp || numericType == NumericType.Mp || numericType == NumericType.Mode || numericType == NumericType.Level)
            {
                return numericType + 1;
            }
            else
            {
                return -1;
            }
        }

        public static void InitializeFromConfig(this global::ET.AttributeComponent self, List<int[]> configData)
        {
            if (configData == null)
            {
                return;
            }

            foreach (int[] item in configData)
            {
                if (item.Length >= 2)
                {
                    self.SetOrAddAttribute(item[0], item[1]);
                }
            }
        }

        public static void InitializeFromConfig(this global::ET.AttributeComponent self, List<float[]> configData)
        {
            if (configData == null)
            {
                return;
            }

            foreach (float[] item in configData)
            {
                if (item.Length >= 2)
                {
                    self.SetOrAddAttribute((int)item[0], item[1]);
                }
            }
        }

        public static Dictionary<int, float> CreateSnapshot(this global::ET.AttributeComponent self)
        {
            Dictionary<int, float> snapshot = new Dictionary<int, float>();
            foreach (AttrCmp attribute in self.GetAllAttributes())
            {
                snapshot[attribute.NumericType] = attribute.ValueFloat;
            }

            return snapshot;
        }

        public static IEnumerable<AttrCmp> GetAllAttributes(this global::ET.AttributeComponent self)
        {
            foreach (int numericType in global::ET.NumericType.GetClientAttributeTypes())
            {
                AttrCmp attribute = self.GetAttrCmp(numericType);
                if (attribute != null)
                {
                    yield return attribute;
                }
            }
        }

        public static void RecalculateAll(this global::ET.AttributeComponent self, ModifierCalculationContext context = null)
        {
            foreach (AttrCmp attribute in self.GetAllAttributes())
            {
                attribute.Recalculate(context);
            }
        }

        public static void Clear(this global::ET.AttributeComponent self)
        {
            List<AttrCmp> attributes = new List<AttrCmp>();
            foreach (AttrCmp attribute in self.GetAllAttributes())
            {
                attributes.Add(attribute);
            }

            foreach (AttrCmp attribute in attributes)
            {
                attribute.ClearModifiers();
                attribute.Dispose();
            }
        }

        private static void SetOrAddAttribute(this global::ET.AttributeComponent self, int numericType, float value)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                attribute = self.AddChildWithId<AttrCmp, int>((long)numericType, numericType);
            }
            attribute.SetBaseValue(value);
        }
    }
}
