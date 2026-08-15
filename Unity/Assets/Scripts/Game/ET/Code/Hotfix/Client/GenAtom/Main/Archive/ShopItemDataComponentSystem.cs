using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(ShopItemDataComponent))]
    [FriendOf(typeof(ShopItemDataComponent))]
    [FriendOf(typeof(PlayerData))]
    public static partial class ShopItemDataComponentSystem
    {
        private const int StackableDefaultCount = 5;
        private const int DefaultCount = 1;

        [EntitySystem]
        private static void Awake(this ShopItemDataComponent self)
        {
            self.Items = new XList<ShopItemData>();
            self.BuildShop();
        }

        [EntitySystem]
        private static void Destroy(this ShopItemDataComponent self)
        {
            self.Items?.Dispose();
            self.Items = null;
        }

        public static void BuildShop(this ShopItemDataComponent self)
        {
            self.BuildShop(0);
        }

        public static void BuildShop(this ShopItemDataComponent self, ItemType itemType)
        {
            self.BuildShop((int)itemType);
        }

        public static void BuildShop(this ShopItemDataComponent self, int itemType)
        {
            IReadOnlyList<DRItems> itemConfigs = Tables.Instance?.DTItems?.DataList;
            if (itemConfigs == null)
            {
                Log.Warning("Shop construction failed: DTItems is not loaded.");
                return;
            }

            self.RebuildShop(itemConfigs, null, itemType);
        }

        public static async UniTask LoadShopData(this ShopItemDataComponent self, ArchiveComponent archiveComponent)
        {
            IReadOnlyList<DRItems> itemConfigs = Tables.Instance?.DTItems?.DataList;
            if (itemConfigs == null)
            {
                Log.Warning("Shop load failed: DTItems is not loaded.");
                return;
            }

            List<ShopItemData> persistedItems = await archiveComponent.QueryAll<ShopItemData>();
            self.RebuildShop(itemConfigs, persistedItems, 0);
            await self.SaveShopData(archiveComponent);
            await self.RemoveObsoleteShopData(archiveComponent);
        }

        public static async UniTask SaveShopData(this ShopItemDataComponent self, ArchiveComponent archiveComponent)
        {
            self.EnsureItems();
            await archiveComponent.SaveBatch(self.Items);
        }

        public static async UniTask RefreshShopData(this ShopItemDataComponent self, ArchiveComponent archiveComponent)
        {
            IReadOnlyList<DRItems> itemConfigs = Tables.Instance?.DTItems?.DataList;
            if (itemConfigs == null)
            {
                Log.Warning("Shop refresh failed: DTItems is not loaded.");
                return;
            }

            self.RebuildShop(itemConfigs, null, 0);
            await self.SaveShopData(archiveComponent);
            await self.RemoveObsoleteShopData(archiveComponent);
        }

        private static void RebuildShop(
            this ShopItemDataComponent self,
            IReadOnlyList<DRItems> itemConfigs,
            IReadOnlyList<ShopItemData> persistedItems,
            int itemType)
        {
            Dictionary<int, ShopItemData> persistedItemByConfigId = new Dictionary<int, ShopItemData>();
            if (persistedItems != null)
            {
                foreach (ShopItemData persistedItem in persistedItems)
                {
                    if (persistedItem == null || Tables.Instance.DTItems.GetOrDefault(persistedItem.ConfigId) == null)
                    {
                        continue;
                    }

                    if (!persistedItemByConfigId.TryGetValue(persistedItem.ConfigId, out ShopItemData currentItem) ||
                        ShouldReplacePersistedItem(currentItem, persistedItem))
                    {
                        persistedItemByConfigId[persistedItem.ConfigId] = persistedItem;
                    }
                }
            }

            self.EnsureItems();
            self.Items.Clear();
            foreach (DRItems itemConfig in itemConfigs)
            {
                if (itemType > 0 && itemConfig.ItemType != itemType)
                {
                    continue;
                }

                int count = persistedItemByConfigId.TryGetValue(itemConfig.Id, out ShopItemData persistedItem)
                    ? persistedItem.Count
                    : GetDefaultCount(itemConfig.ItemType);
                self.Items.Add(new ShopItemData
                {
                    Id = itemConfig.Id,
                    ConfigId = itemConfig.Id,
                    Count = count,
                });
            }
        }

        private static async UniTask RemoveObsoleteShopData(
            this ShopItemDataComponent self,
            ArchiveComponent archiveComponent)
        {
            HashSet<long> activeItemIds = new HashSet<long>();
            foreach (ShopItemData item in self.Items)
            {
                activeItemIds.Add(item.Id);
            }

            List<ShopItemData> persistedItems = await archiveComponent.QueryAll<ShopItemData>();
            foreach (ShopItemData persistedItem in persistedItems)
            {
                if (persistedItem != null && !activeItemIds.Contains(persistedItem.Id))
                {
                    await archiveComponent.Remove<ShopItemData>(persistedItem.Id);
                }
            }
        }

        public static XList<ShopItemData> GetItems(this ShopItemDataComponent self)
        {
            self.EnsureItems();
            return self.Items;
        }

        public static List<ShopItemData> GetItems(this ShopItemDataComponent self, ItemType itemType)
        {
            return self.GetItems((int)itemType);
        }

        public static List<ShopItemData> GetItems(this ShopItemDataComponent self, int itemType)
        {
            self.EnsureItems();
            List<ShopItemData> result = new List<ShopItemData>();
            foreach (ShopItemData item in self.Items)
            {
                DRItems itemConfig = Tables.Instance.DTItems.GetOrDefault(item.ConfigId);
                if (itemConfig != null && itemConfig.ItemType == itemType)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        public static bool CanBuy(this ShopItemDataComponent self, ShopItemData item, PlayerData playerData)
        {
            self.EnsureItems();
            if (item == null || playerData == null || item.Count <= 0 || !self.Items.Contains(item))
            {
                return false;
            }

            DRItems itemConfig = Tables.Instance?.DTItems?.GetOrDefault(item.ConfigId);
            return itemConfig != null && itemConfig.Diamond >= 0 && playerData.Diamond >= itemConfig.Diamond;
        }

        public static bool TryBuy(
            this ShopItemDataComponent self,
            ShopItemData item,
            PlayerData playerData,
            InventoryDataComponent inventoryDataComponent)
        {
            return self.TryBuy(
                item,
                playerData,
                inventoryDataComponent,
                out _,
                out _,
                out _,
                out _,
                out _);
        }

        public static bool TryBuy(
            this ShopItemDataComponent self,
            ShopItemData item,
            PlayerData playerData,
            InventoryDataComponent inventoryDataComponent,
            out InventoryItemData inventoryItem,
            out bool inventoryItemCreated,
            out int previousInventoryItemCount,
            out int previousPlayerDiamond,
            out int previousShopItemCount)
        {
            inventoryItem = null;
            inventoryItemCreated = false;
            previousInventoryItemCount = 0;
            previousPlayerDiamond = 0;
            previousShopItemCount = 0;
            if (inventoryDataComponent == null || !self.CanBuy(item, playerData))
            {
                return false;
            }

            DRItems itemConfig = Tables.Instance.DTItems.Get(item.ConfigId);
            previousPlayerDiamond = playerData.Diamond;
            previousShopItemCount = item.Count;
            int inventoryItemCount = inventoryDataComponent.GetItems().Count;
            inventoryItem = inventoryDataComponent.AddItem(item.ConfigId);
            if (inventoryItem == null)
            {
                return false;
            }

            inventoryItemCreated = inventoryDataComponent.GetItems().Count > inventoryItemCount;
            previousInventoryItemCount = inventoryItemCreated ? 0 : inventoryItem.Count - 1;
            playerData.Diamond -= itemConfig.Diamond;
            item.Count--;
            return true;
        }

        public static void RollbackBuy(
            this ShopItemDataComponent self,
            ShopItemData item,
            PlayerData playerData,
            InventoryDataComponent inventoryDataComponent,
            InventoryItemData inventoryItem,
            bool inventoryItemCreated,
            int previousInventoryItemCount,
            int previousPlayerDiamond,
            int previousShopItemCount)
        {
            if (playerData != null)
            {
                playerData.Diamond = previousPlayerDiamond;
            }

            if (item != null)
            {
                item.Count = previousShopItemCount;
            }

            if (inventoryDataComponent != null && inventoryItem != null)
            {
                if (inventoryItemCreated)
                {
                    inventoryDataComponent.GetItems().Remove(inventoryItem);
                }
                else
                {
                    inventoryItem.Count = previousInventoryItemCount;
                }
            }
        }

        private static bool ShouldReplacePersistedItem(ShopItemData currentItem, ShopItemData candidateItem)
        {
            bool currentIsStable = currentItem.Id == currentItem.ConfigId;
            bool candidateIsStable = candidateItem.Id == candidateItem.ConfigId;
            if (currentIsStable || candidateIsStable)
            {
                return candidateIsStable && !currentIsStable;
            }

            return candidateItem.Count < currentItem.Count ||
                    candidateItem.Count == currentItem.Count && candidateItem.Id > currentItem.Id;
        }

        private static int GetDefaultCount(int itemType)
        {
            return itemType == (int)ItemType.Herb || itemType == (int)ItemType.Medicine
                ? StackableDefaultCount
                : DefaultCount;
        }

        private static void EnsureItems(this ShopItemDataComponent self)
        {
            self.Items ??= new XList<ShopItemData>();
        }
    }
}
