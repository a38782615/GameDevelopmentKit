using System.Collections.Generic;

namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.AttributeComponent))]
    public static class AttributeComponentAccessorSystem
    {
        public static Attribute GetAttribute(this global::ET.AttributeComponent self, AttrType attrType)
        {
            return self.TryGetAttribute(attrType, out Attribute attribute) ? attribute : null;
        }

        public static bool RemoveAttribute(this global::ET.AttributeComponent self, AttrType attrType)
        {
            Dictionary<AttrType, Attribute> attributes = self.EnsureAttributeMap();
            if (!attributes.TryGetValue(attrType, out Attribute attribute))
            {
                return false;
            }

            attribute.ClearCallbacks();
            attribute.ClearModifiers();
            attributes.Remove(attrType);
            return true;
        }

        public static bool HasAttribute(this global::ET.AttributeComponent self, AttrType attrType)
        {
            return self.EnsureAttributeMap().ContainsKey(attrType);
        }

        public static float GetBaseValue(this global::ET.AttributeComponent self, AttrType attrType)
        {
            return self.GetAttribute(attrType)?.BaseValue ?? 0f;
        }

        public static float GetCurrentValue(this global::ET.AttributeComponent self, AttrType attrType)
        {
            return self.GetAttribute(attrType)?.CurrentValue ?? 0f;
        }

        public static bool SetBaseValue(this global::ET.AttributeComponent self, AttrType attrType, float value)
        {
            Attribute attribute = self.GetAttribute(attrType);
            if (attribute == null)
            {
                return false;
            }

            attribute.BaseValue = value;
            return true;
        }

        public static bool SetCurrentValue(this global::ET.AttributeComponent self, AttrType attrType, float value)
        {
            Attribute attribute = self.GetAttribute(attrType);
            if (attribute == null)
            {
                return false;
            }

            attribute.CurrentValue = value;
            return true;
        }

        public static bool InitializeAttribute(this global::ET.AttributeComponent self, AttrType attrType, float value)
        {
            Attribute attribute = self.GetAttribute(attrType);
            if (attribute == null)
            {
                return false;
            }

            attribute.Initialize(value);
            return true;
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
                    self.SetOrAddAttribute((AttrType)item[0], item[1]);
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
                    self.SetOrAddAttribute((AttrType)(int)item[0], item[1]);
                }
            }
        }

        public static Dictionary<AttrType, float> CreateSnapshot(this global::ET.AttributeComponent self)
        {
            Dictionary<AttrType, float> snapshot = new Dictionary<AttrType, float>();
            foreach (KeyValuePair<AttrType, Attribute> pair in self.EnsureAttributeMap())
            {
                snapshot[pair.Key] = pair.Value.CurrentValue;
            }

            return snapshot;
        }

        public static IEnumerable<Attribute> GetAllAttributes(this global::ET.AttributeComponent self)
        {
            return self.EnsureAttributeMap().Values;
        }

        public static void RecalculateAll(this global::ET.AttributeComponent self, ModifierCalculationContext context = null)
        {
            foreach (Attribute attribute in self.EnsureAttributeMap().Values)
            {
                attribute.Recalculate(context);
            }
        }

        public static void Clear(this global::ET.AttributeComponent self)
        {
            Dictionary<AttrType, Attribute> attributes = self.EnsureAttributeMap();
            foreach (Attribute attribute in attributes.Values)
            {
                attribute.ClearCallbacks();
                attribute.ClearModifiers();
            }

            attributes.Clear();
        }

        private static void SetOrAddAttribute(this global::ET.AttributeComponent self, AttrType attrType, float value)
        {
            if (!self.InitializeAttribute(attrType, value) && !self.HasAttribute(attrType))
            {
                Dictionary<AttrType, Attribute> attributes = self.EnsureAttributeMap();
                attributes[attrType] = new Attribute(attrType, value);
            }
        }

        private static Dictionary<AttrType, Attribute> EnsureAttributeMap(this global::ET.AttributeComponent self)
        {
            if (self.RuntimeAttributes is not Dictionary<AttrType, Attribute> attributes)
            {
                attributes = new Dictionary<AttrType, Attribute>();
                self.RuntimeAttributes = attributes;
            }

            return attributes;
        }

        private static bool TryGetAttribute(this global::ET.AttributeComponent self, AttrType attrType, out Attribute attribute)
        {
            return self.EnsureAttributeMap().TryGetValue(attrType, out attribute);
        }
    }
}
