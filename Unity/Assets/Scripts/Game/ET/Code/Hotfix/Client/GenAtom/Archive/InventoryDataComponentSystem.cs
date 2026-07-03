using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(InventoryDataComponent))]
    [FriendOf(typeof(InventoryDataComponent))]
    public static partial class InventoryDataComponentSystem
    {
        [EntitySystem]
        private static void Awake(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
        }

        [EntitySystem]
        private static void Destroy(this InventoryDataComponent self)
        {
            self.Items?.Clear();
            self.Items = null;
            self.SlotToItemId?.Clear();
            self.SlotToItemId = null;
        }

        public static async UniTask LoadInventoryData(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            List<InventoryItemData> itemDatas = await archiveComponent.QueryAll<InventoryItemData>();
            self.RebuildInventoryItemCache(itemDatas);
            self.RefreshUnitEquipAttributes();
        }

        public static async UniTask SaveInventoryData(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureInventoryItemCache();
            await self.SaveInventoryItems(archiveComponent);
        }

        public static InventoryItemData AddItem(this InventoryDataComponent self, int configId, int count)
        {
            self.EnsureInventoryItemCache();
            if (count <= 0)
            {
                return null;
            }

            if (Tables.Instance.DTItems.GetOrDefault(configId) == null)
            {
                return null;
            }

            InventoryItemData itemData = new InventoryItemData
            {
                Id = IdGenerater.Instance.GenerateId(),
                ConfigId = configId,
                Count = count,
                IsEquipped = false,
                EquipSlot = 0,
            };
            self.Items[itemData.Id] = itemData;
            return itemData;
        }

        public static bool RemoveItem(this InventoryDataComponent self, long itemId, int count)
        {
            self.EnsureInventoryItemCache();
            if (!self.Items.TryGetValue(itemId, out InventoryItemData itemData))
            {
                return false;
            }

            if (count <= 0 || itemData.Count < count)
            {
                return false;
            }

            itemData.Count -= count;
            if (itemData.Count == 0)
            {
                self.Items.Remove(itemId);
                if (itemData.IsEquipped)
                {
                    self.SlotToItemId.Remove(itemData.EquipSlot);
                    self.RefreshUnitEquipAttributes();
                }
            }

            return true;
        }

        public static bool EquipItem(this InventoryDataComponent self, long itemId, int slot)
        {
            self.EnsureInventoryItemCache();
            if (!self.Items.TryGetValue(itemId, out InventoryItemData itemData))
            {
                return false;
            }

            if (!IsValidEquipSlot(slot))
            {
                return false;
            }

            DRItems itemConfig = Tables.Instance.DTItems.GetOrDefault(itemData.ConfigId);
            if (itemConfig == null || itemConfig.ItemType != 2)
            {
                return false;
            }

            if (itemData.IsEquipped)
            {
                self.SlotToItemId.Remove(itemData.EquipSlot);
            }

            if (self.SlotToItemId.TryGetValue(slot, out long oldItemId))
            {
                if (self.Items.TryGetValue(oldItemId, out InventoryItemData oldItemData))
                {
                    oldItemData.IsEquipped = false;
                    oldItemData.EquipSlot = 0;
                }

                self.SlotToItemId.Remove(slot);
            }

            itemData.IsEquipped = true;
            itemData.EquipSlot = slot;
            self.SlotToItemId[slot] = itemId;
            self.RefreshUnitEquipAttributes();
            return true;
        }

        public static bool UnequipItem(this InventoryDataComponent self, int slot)
        {
            self.EnsureInventoryItemCache();
            if (!self.SlotToItemId.TryGetValue(slot, out long itemId))
            {
                return false;
            }

            if (self.Items.TryGetValue(itemId, out InventoryItemData itemData))
            {
                itemData.IsEquipped = false;
                itemData.EquipSlot = 0;
            }

            self.SlotToItemId.Remove(slot);
            self.RefreshUnitEquipAttributes();
            return true;
        }

        public static InventoryItemData GetItem(this InventoryDataComponent self, long itemId)
        {
            self.EnsureInventoryItemCache();
            return self.Items.TryGetValue(itemId, out InventoryItemData itemData) ? itemData : null;
        }

        public static IReadOnlyCollection<InventoryItemData> GetAllItems(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
            return self.Items.Values;
        }

        public static IReadOnlyDictionary<long, InventoryItemData> GetItemMap(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
            return self.Items;
        }

        public static IReadOnlyDictionary<int, long> GetEquippedSlotToItemIds(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
            return self.SlotToItemId;
        }

        private static void RebuildInventoryItemCache(this InventoryDataComponent self, List<InventoryItemData> itemDatas)
        {
            self.EnsureInventoryItemCache();
            self.Items.Clear();
            self.SlotToItemId.Clear();
            if (itemDatas == null)
            {
                return;
            }

            foreach (InventoryItemData itemData in itemDatas)
            {
                if (itemData == null || itemData.Id <= 0)
                {
                    continue;
                }

                self.Items[itemData.Id] = itemData;
            }

            self.RebuildEquipSlotCache();
        }

        private static void EnsureInventoryItemCache(this InventoryDataComponent self)
        {
            self.Items ??= new Dictionary<long, InventoryItemData>();
            self.SlotToItemId ??= new Dictionary<int, long>();
            self.NormalizeInventoryItemIds();
            self.RebuildEquipSlotCache();
        }

        private static async UniTask SaveInventoryItems(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            List<InventoryItemData> itemDatas = new List<InventoryItemData>(self.Items.Values);
            foreach (InventoryItemData itemData in itemDatas)
            {
                if (itemData == null)
                {
                    continue;
                }

                await archiveComponent.Save(itemData.Id, itemData);
            }
        }

        private static void NormalizeInventoryItemIds(this InventoryDataComponent self)
        {
            List<long> removeItemIds = null;
            List<InventoryItemData> addItemDatas = null;
            foreach (KeyValuePair<long, InventoryItemData> kv in self.Items)
            {
                InventoryItemData itemData = kv.Value;
                if (itemData == null)
                {
                    removeItemIds ??= new List<long>();
                    removeItemIds.Add(kv.Key);
                    continue;
                }

                if (itemData.Id <= 0)
                {
                    itemData.Id = IdGenerater.Instance.GenerateId();
                }

                if (itemData.Id != kv.Key)
                {
                    removeItemIds ??= new List<long>();
                    addItemDatas ??= new List<InventoryItemData>();
                    removeItemIds.Add(kv.Key);
                    addItemDatas.Add(itemData);
                }
            }

            if (removeItemIds != null)
            {
                foreach (long itemId in removeItemIds)
                {
                    self.Items.Remove(itemId);
                }
            }

            if (addItemDatas != null)
            {
                foreach (InventoryItemData itemData in addItemDatas)
                {
                    self.Items[itemData.Id] = itemData;
                }
            }
        }

        private static void RebuildEquipSlotCache(this InventoryDataComponent self)
        {
            self.SlotToItemId.Clear();
            foreach (InventoryItemData itemData in self.Items.Values)
            {
                if (itemData == null || !itemData.IsEquipped || !IsValidEquipSlot(itemData.EquipSlot))
                {
                    continue;
                }

                self.SlotToItemId[itemData.EquipSlot] = itemData.Id;
            }
        }

        private static bool IsValidEquipSlot(int slot)
        {
            return slot >= 0;
        }

        private static void RefreshUnitEquipAttributes(this InventoryDataComponent self)
        {
            Scene currentScene = self.Root()?.CurrentScene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                return;
            }

            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(currentScene);
            if (unit == null)
            {
                return;
            }

            EquipComponent equipComponent = unit.GetComponent<EquipComponent>();
            if (equipComponent != null)
            {
                equipComponent.RefreshFromItems(self.Items, self.SlotToItemId);
            }
        }
    }
}
