using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(InventoryDataComponent))]
    [FriendOf(typeof(InventoryDataComponent))]
    public static partial class InventoryDataComponentSystem
    {
        private const string InventoryDataDocumentId = nameof(InventoryData);

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
            InventoryData inventoryData = await archiveComponent.QueryById<InventoryData>(InventoryDataDocumentId);
            if (inventoryData == null)
            {
                inventoryData = CreateDefaultInventoryData();
                await archiveComponent.Save(InventoryDataDocumentId, inventoryData);
            }

            EnsureInventoryData(inventoryData);
            self.InventoryData = inventoryData;
            self.RefreshUnitEquipAttributes();
        }

        public static async UniTask SaveInventoryData(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            if (self.InventoryData == null)
            {
                return;
            }

            EnsureInventoryData(self.InventoryData);
            await archiveComponent.Save(InventoryDataDocumentId, self.InventoryData);
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
                Id = inventoryData.NextItemId++,
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

        private static void EnsureInventoryData(InventoryData inventoryData)
        {
            inventoryData.BagData ??= new InventoryBagData();
            inventoryData.BagData.Items ??= new Dictionary<long, InventoryItemData>();
            inventoryData.EquipData ??= new InventoryEquipData();
            inventoryData.EquipData.SlotToItemId ??= new Dictionary<int, long>();
            if (inventoryData.NextItemId <= 0)
            {
                inventoryData.NextItemId = 1;
            }
        }

        private static bool IsValidEquipSlot(int slot)
        {
            return slot >= 0;
        }

        private static void RefreshUnitEquipAttributes(this InventoryDataComponent self)
        {
            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(self.Root().CurrentScene());
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
