using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(InventoryDataComponent))]
    [FriendOf(typeof(InventoryDataComponent))]
    public static partial class InventoryDataComponentSystem
    {
        private const string LegacyInventoryDataDocumentId = nameof(InventoryData);

        [EntitySystem]
        private static void Awake(this InventoryDataComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this InventoryDataComponent self)
        {
            self.InventoryData = null;
        }

        public static async UniTask LoadInventoryData(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            List<InventoryItemData> itemDatas = await archiveComponent.QueryAll<InventoryItemData>();
            InventoryData legacyInventoryData = await archiveComponent.QueryById<InventoryData>(LegacyInventoryDataDocumentId);
            bool needSaveMigratedItems = false;
            if ((itemDatas == null || itemDatas.Count == 0) && legacyInventoryData != null)
            {
                EnsureInventoryData(legacyInventoryData);
                itemDatas = new List<InventoryItemData>(legacyInventoryData.BagData.Items.Values);
                needSaveMigratedItems = itemDatas.Count > 0;
            }

            InventoryData inventoryData = CreateDefaultInventoryData();
            RebuildInventoryDataCache(inventoryData, itemDatas);
            EnsureInventoryData(inventoryData);
            self.InventoryData = inventoryData;
            self.RefreshUnitEquipAttributes();

            if (needSaveMigratedItems)
            {
                await self.SaveInventoryItems(archiveComponent);
            }

            if (legacyInventoryData != null)
            {
                await archiveComponent.Remove<InventoryData>(LegacyInventoryDataDocumentId);
            }
        }

        public static async UniTask SaveInventoryData(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            if (self.InventoryData == null)
            {
                return;
            }

            EnsureInventoryData(self.InventoryData);
            await self.SaveInventoryItems(archiveComponent);
            await self.RemoveStaleInventoryItems(archiveComponent);
            await archiveComponent.Remove<InventoryData>(LegacyInventoryDataDocumentId);
        }

        public static InventoryItemData AddItem(this InventoryDataComponent self, int configId, int count)
        {
            InventoryData inventoryData = self.GetOrCreateInventoryData();
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
            inventoryData.BagData.Items[itemData.Id] = itemData;
            return itemData;
        }

        public static bool RemoveItem(this InventoryDataComponent self, long itemId, int count)
        {
            InventoryData inventoryData = self.GetOrCreateInventoryData();
            if (!inventoryData.BagData.Items.TryGetValue(itemId, out InventoryItemData itemData))
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
                inventoryData.BagData.Items.Remove(itemId);
                if (itemData.IsEquipped)
                {
                    inventoryData.EquipData.SlotToItemId.Remove(itemData.EquipSlot);
                    self.RefreshUnitEquipAttributes();
                }
            }

            return true;
        }

        public static bool EquipItem(this InventoryDataComponent self, long itemId, int slot)
        {
            InventoryData inventoryData = self.GetOrCreateInventoryData();
            if (!inventoryData.BagData.Items.TryGetValue(itemId, out InventoryItemData itemData))
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
                inventoryData.EquipData.SlotToItemId.Remove(itemData.EquipSlot);
            }

            if (inventoryData.EquipData.SlotToItemId.TryGetValue(slot, out long oldItemId))
            {
                if (inventoryData.BagData.Items.TryGetValue(oldItemId, out InventoryItemData oldItemData))
                {
                    oldItemData.IsEquipped = false;
                    oldItemData.EquipSlot = 0;
                }

                inventoryData.EquipData.SlotToItemId.Remove(slot);
            }

            itemData.IsEquipped = true;
            itemData.EquipSlot = slot;
            inventoryData.EquipData.SlotToItemId[slot] = itemId;
            self.RefreshUnitEquipAttributes();
            return true;
        }

        public static bool UnequipItem(this InventoryDataComponent self, int slot)
        {
            InventoryData inventoryData = self.GetOrCreateInventoryData();
            if (!inventoryData.EquipData.SlotToItemId.TryGetValue(slot, out long itemId))
            {
                return false;
            }

            if (inventoryData.BagData.Items.TryGetValue(itemId, out InventoryItemData itemData))
            {
                itemData.IsEquipped = false;
                itemData.EquipSlot = 0;
            }

            inventoryData.EquipData.SlotToItemId.Remove(slot);
            self.RefreshUnitEquipAttributes();
            return true;
        }

        public static InventoryItemData GetItem(this InventoryDataComponent self, long itemId)
        {
            InventoryData inventoryData = self.GetOrCreateInventoryData();
            return inventoryData.BagData.Items.TryGetValue(itemId, out InventoryItemData itemData) ? itemData : null;
        }

        public static IReadOnlyCollection<InventoryItemData> GetAllItems(this InventoryDataComponent self)
        {
            return self.GetOrCreateInventoryData().BagData.Items.Values;
        }

        private static InventoryData GetOrCreateInventoryData(this InventoryDataComponent self)
        {
            if (self.InventoryData == null)
            {
                self.InventoryData = CreateDefaultInventoryData();
            }

            EnsureInventoryData(self.InventoryData);
            return self.InventoryData;
        }

        private static InventoryData CreateDefaultInventoryData()
        {
            return new InventoryData();
        }

        private static void RebuildInventoryDataCache(InventoryData inventoryData, List<InventoryItemData> itemDatas)
        {
            EnsureInventoryData(inventoryData);
            inventoryData.BagData.Items.Clear();
            inventoryData.EquipData.SlotToItemId.Clear();
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

                inventoryData.BagData.Items[itemData.Id] = itemData;
                if (itemData.IsEquipped && IsValidEquipSlot(itemData.EquipSlot))
                {
                    inventoryData.EquipData.SlotToItemId[itemData.EquipSlot] = itemData.Id;
                }
            }
        }

        private static void EnsureInventoryData(InventoryData inventoryData)
        {
            inventoryData.BagData ??= new InventoryBagData();
            inventoryData.BagData.Items ??= new Dictionary<long, InventoryItemData>();
            inventoryData.EquipData ??= new InventoryEquipData();
            inventoryData.EquipData.SlotToItemId ??= new Dictionary<int, long>();
            NormalizeInventoryItemIds(inventoryData);
            RebuildEquipSlotCache(inventoryData);
        }

        private static async UniTask SaveInventoryItems(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            List<InventoryItemData> itemDatas = new List<InventoryItemData>(self.InventoryData.BagData.Items.Values);
            foreach (InventoryItemData itemData in itemDatas)
            {
                if (itemData == null)
                {
                    continue;
                }

                await archiveComponent.Save(itemData.Id, itemData);
            }
        }

        private static async UniTask RemoveStaleInventoryItems(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            List<InventoryItemData> persistedItems = await archiveComponent.QueryAll<InventoryItemData>();
            if (persistedItems == null || persistedItems.Count == 0)
            {
                return;
            }

            foreach (InventoryItemData itemData in persistedItems)
            {
                if (itemData != null && !self.InventoryData.BagData.Items.ContainsKey(itemData.Id))
                {
                    await archiveComponent.Remove<InventoryItemData>(itemData.Id);
                }
            }
        }

        private static void NormalizeInventoryItemIds(InventoryData inventoryData)
        {
            List<long> removeItemIds = null;
            List<InventoryItemData> addItemDatas = null;
            foreach (KeyValuePair<long, InventoryItemData> kv in inventoryData.BagData.Items)
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
                    inventoryData.BagData.Items.Remove(itemId);
                }
            }

            if (addItemDatas != null)
            {
                foreach (InventoryItemData itemData in addItemDatas)
                {
                    inventoryData.BagData.Items[itemData.Id] = itemData;
                }
            }
        }

        private static void RebuildEquipSlotCache(InventoryData inventoryData)
        {
            inventoryData.EquipData.SlotToItemId.Clear();
            foreach (InventoryItemData itemData in inventoryData.BagData.Items.Values)
            {
                if (itemData == null || !itemData.IsEquipped || !IsValidEquipSlot(itemData.EquipSlot))
                {
                    continue;
                }

                inventoryData.EquipData.SlotToItemId[itemData.EquipSlot] = itemData.Id;
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
                equipComponent.RefreshFromItems(self.InventoryData);
            }
        }
    }
}
