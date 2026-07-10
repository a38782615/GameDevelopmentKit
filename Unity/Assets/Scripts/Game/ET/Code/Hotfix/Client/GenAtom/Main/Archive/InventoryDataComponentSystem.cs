using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(InventoryDataComponent))]
    [FriendOf(typeof(InventoryDataComponent))]
    [FriendOfAttribute(typeof(ET.Client.ArchiveMgrComponent))]

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

        public static async UniTask SaveInventoryData(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
            var archiveComponent = self.Root().GetComponent<ArchiveMgrComponent>().CurrentArchive.As();
            await self.SaveInventoryItems(archiveComponent);
        }

        private static bool CanStack(this InventoryDataComponent self, int configId)
        {
            var itemConfig = Tables.Instance.DTItems.GetOrDefault(configId);
            return itemConfig.ItemType == 1;
        }

        public static InventoryItemData AddItem(this InventoryDataComponent self, int configId, int count = 1)
        {
            self.EnsureInventoryItemCache();
            if (count <= 0)
            {
                return null;
            }
            var itemData = self.Items.Find(x => x.ConfigId == configId);
            if (self.CanStack(configId) && itemData!=null)
            {
                itemData.Count += count;
            }
            else
            {
                itemData = new InventoryItemData()
                {
                    Id = IdGenerater.Instance.GenerateId(),
                    ConfigId = configId,
                    Count = count,
                };
                self.Items.Add(itemData);
            }
            return itemData;
        }

        public static bool RemoveItem(this InventoryDataComponent self, InventoryItemData itemData)
        {
            self.EnsureInventoryItemCache(); 
            self.Items.Remove(itemData);
            if (itemData.IsEquipped)
            {
                self.SlotToItemId.Remove(itemData.EquipSlot);
                self.RefreshUnitEquipAttributes();
            }
            var archiveComponent = self.Root().GetComponent<ArchiveMgrComponent>().CurrentArchive.As();
            archiveComponent.Remove<InventoryItemData>(itemData.Id).Forget();
            return true;
        }

        public static bool EquipItem(this InventoryDataComponent self, long configId, int slot)
        {
            self.EnsureInventoryItemCache();
            var itemData = self.Items.Find(x => x.ConfigId == configId);
            if (itemData == null)
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
                var oldItemData = self.Items.Find(x => x.ConfigId == oldItemId);
                if (oldItemData!=null)
                {
                    oldItemData.IsEquipped = false;
                    oldItemData.EquipSlot = 0;
                }

                self.SlotToItemId.Remove(slot);
            }

            itemData.IsEquipped = true;
            itemData.EquipSlot = slot;
            self.SlotToItemId[slot] = configId;
            self.RefreshUnitEquipAttributes();
            return true;
        }

        public static bool UnequipItem(this InventoryDataComponent self, int slot)
        {
            self.EnsureInventoryItemCache();
            if (!self.SlotToItemId.TryGetValue(slot, out long configId))
            {
                return false;
            }

            var itemData = self.Items.Find(x => x.ConfigId == configId);
            if (itemData!=null)
            {
                itemData.IsEquipped = false;
                itemData.EquipSlot = 0;
            }

            self.SlotToItemId.Remove(slot);
            self.RefreshUnitEquipAttributes();
            return true;
        }
        public static XList<InventoryItemData> GetItems(this InventoryDataComponent self)
        {
            self.EnsureInventoryItemCache();
            return self.Items;
        }

        public static XDictionary<int, long> GetEquippedSlotToItemIds(this InventoryDataComponent self)
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

                if (itemData.Type1 == 0)
                {
                    self.Items.Add(itemData);
                }
                else if (itemData.Type1 == 0)
                {
                    self.BagItems.Add(itemData);
                }
            }

            self.RebuildEquipSlotCache();
        }

        private static void EnsureInventoryItemCache(this InventoryDataComponent self)
        {
            self.RebuildEquipSlotCache();
        }

        private static async UniTask SaveInventoryItems(this InventoryDataComponent self, ArchiveComponent archiveComponent)
        {
            await archiveComponent.SaveBatch(self.Items);
            await archiveComponent.SaveBatch(self.BagItems);
        }
  
        private static void RebuildEquipSlotCache(this InventoryDataComponent self)
        {
            self.SlotToItemId.Clear();
            foreach (var k in self.Items)
            {
                var itemData = k;
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

        public static void DropToBag(this InventoryDataComponent self, InventoryItemData data)
        {
            self.AddItem(data.ConfigId, data.Count);
            var drops = self.Drops;
            drops?.Remove(data);
            self.SaveInventoryData().Forget();
        }

        public static void BagToDrop(this InventoryDataComponent self, InventoryItemData data)
        {
            self.RemoveItem(data);
            self.AddDrop(data.ConfigId, data.Count);
            self.SaveInventoryData().Forget();
        }

        public static void AddDrop(this InventoryDataComponent self, int configId, int count = 1)
        {
            var drops = self.Drops;
            var d = drops.Find(x => x.ConfigId == configId);
            if (d!=null && self.CanStack(configId))
            {
                d.Count += count;
            }
            else
            {
                var di = new InventoryItemData()
                {
                    Id = IdGenerater.Instance.GenerateId(),
                    ConfigId = configId,
                    Count = count,
                    Type1 = -1
                };
                drops.Add(di);
            }
        }
    }
}
