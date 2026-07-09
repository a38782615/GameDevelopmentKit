using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(EquipComponent))]
    [FriendOf(typeof(EquipComponent))]
    [FriendOf(typeof(global::ET.AttributeComponent))]
    [FriendOf(typeof(NumericComponent))]
    public static partial class EquipComponentSystem
    {
        [EntitySystem]
        private static void Awake(this EquipComponent self)
        {
            self.EquipModifiers = XDictionary<int, List<DataModifier>>.Create();
            self.All = XList<DataModifier>.Create();
            self.DataId = 1;
        }

        [EntitySystem]
        private static void Destroy(this EquipComponent self)
        {
            self.ClearAllModifiers();
            self.EquipModifiers?.Dispose();
            self.All?.Dispose();
        }

        public static DataModifier AddModifier(this EquipComponent self, int type, float value)
        {
            DataModifier modify = DataModifier.Create(self.DataId++, type, value);
            self.All.Add(modify);

            if (!self.EquipModifiers.TryGetValue(type, out List<DataModifier> modifiers))
            {
                modifiers = new List<DataModifier>();
                self.EquipModifiers[type] = modifiers;
            }

            modifiers.Add(modify);
            self.CountAttr(type);
            return modify;
        }

        public static void RemoveModifer(this EquipComponent self, DataModifier modify)
        {
            if (modify == null)
            {
                return;
            }

            if (self.All != null)
            {
                self.All.Remove(modify);
            }

            if (self.EquipModifiers != null && self.EquipModifiers.TryGetValue(modify.Attribute, out List<DataModifier> modifiers))
            {
                modifiers.Remove(modify);
                if (modifiers.Count == 0)
                {
                    self.EquipModifiers.Remove(modify.Attribute);
                }
            }

            self.CountAttr(modify.Attribute);
            modify.Dispose();
        }

        public static void ClearAllModifiers(this EquipComponent self)
        {
            if (self.All == null || self.All.Count == 0)
            {
                return;
            }

            List<DataModifier> modifiers = new List<DataModifier>(self.All);
            foreach (DataModifier modify in modifiers)
            {
                modify.Dispose();
            }

            self.All.Clear();
            self.EquipModifiers?.Clear();
        }

        public static void RefreshFromItems(
            this EquipComponent self,
            XDictionary<long, InventoryItemData> items,
            XDictionary<int, long> slotToItemId)
        {
            HashSet<int> affectedTypes = new HashSet<int>();
            if (self.All != null)
            {
                foreach (DataModifier modify in self.All)
                {
                    affectedTypes.Add(modify.Attribute);
                }
            }

            self.ClearAllModifiers();

            if (items == null || slotToItemId == null)
            {
                foreach (int attributeType in affectedTypes)
                {
                    self.CountAttr(attributeType);
                }
                return;
            }

            foreach (KeyValuePair<int, long> kv in slotToItemId)
            {
                if (!items.TryGetValue(kv.Value, out InventoryItemData itemData))
                {
                    continue;
                }

                if (itemData == null || !itemData.IsEquipped)
                {
                    continue;
                }

                DRItems itemConfig = Tables.Instance.DTItems.GetOrDefault(itemData.ConfigId);
                if (itemConfig?.Attr == null)
                {
                    continue;
                }

                foreach (KeyValuePair<int, long> attr in itemConfig.Attr)
                {
                    self.AddModifier(attr.Key, attr.Value);
                    affectedTypes.Add(attr.Key);
                }
            }

            foreach (int attributeType in affectedTypes)
            {
                self.CountAttr(attributeType);
            }
        }

        private static void CountAttr(this EquipComponent self, int attributeType)
        {
            AttributeComponent attributeComponent = self.GetParent<Unit>().GetComponent<AttributeComponent>();
            if (attributeComponent == null || attributeComponent.NumericComponent == null)
            {
                return;
            }

            float value = 0f;
            if (self.EquipModifiers != null && self.EquipModifiers.TryGetValue(attributeType, out List<DataModifier> modifiers))
            {
                foreach (DataModifier modifier in modifiers)
                {
                    value += modifier.Value;
                }
            }

            attributeComponent.NumericComponent.Set(attributeType, value);
        }
    }
}
