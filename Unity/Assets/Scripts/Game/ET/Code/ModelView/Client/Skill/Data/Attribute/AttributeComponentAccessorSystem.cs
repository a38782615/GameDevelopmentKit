using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// AttributeComponent 的只读/读写访问入口。
    /// 这里屏蔽 AttrCmp 子实体的创建与查询细节，业务层统一按 NumericType 访问。
    /// </summary>
    [FriendOfAttribute(typeof(global::ET.AttributeComponent))]
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

            attribute.ClearCallbacks();
            attribute.ClearModifiers();
            attribute.Dispose();
            return true;
        }

        public static bool HasAttribute(this global::ET.AttributeComponent self, int numericType)
        {
            return self.GetAttrCmp(numericType) != null;
        }

        public static float GetBaseValue(this global::ET.AttributeComponent self, int numericType)
        {
            return self.GetAttrCmp(numericType)?.BaseValue ?? 0f;
        }

        public static float GetCurrentValue(this global::ET.AttributeComponent self, int numericType)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute != null)
            {
                return attribute.CurrentValue;
            }

            NumericComponent numericComponent = self?.NumericComponent;
            return numericComponent == null ? 0f : numericComponent.GetAsFloat(numericType);
        }

        public static bool SetBaseValue(this global::ET.AttributeComponent self, int numericType, float value)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                return false;
            }

            attribute.BaseValue = value;
            return true;
        }

        public static bool SetCurrentValue(this global::ET.AttributeComponent self, int numericType, float value)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
            if (attribute == null)
            {
                return false;
            }

            attribute.CurrentValue = value;
            return true;
        }

        public static bool InitializeAttribute(this global::ET.AttributeComponent self, int numericType, float value)
        {
            AttrCmp attribute = self.GetAttrCmp(numericType);
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
            // Snapshot 按 NumericType 存值，供 MMC 和公式计算复用。
            Dictionary<int, float> snapshot = new Dictionary<int, float>();
            foreach (AttrCmp attribute in self.GetAllAttributes())
            {
                snapshot[attribute.NumericType] = attribute.CurrentValue;
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
                attribute.ClearCallbacks();
                attribute.ClearModifiers();
                attribute.Dispose();
            }
        }

        private static void SetOrAddAttribute(this global::ET.AttributeComponent self, int numericType, float value)
        {
            if (!self.InitializeAttribute(numericType, value) && !self.HasAttribute(numericType))
            {
                // ModelView 侧不能依赖 HotfixView 扩展方法，缺失时直接补子实体。
                AttrCmp attribute = self.AddChildWithId<AttrCmp, int>((long)numericType, numericType);
                attribute.Initialize(value);
            }
        }
    }
}
