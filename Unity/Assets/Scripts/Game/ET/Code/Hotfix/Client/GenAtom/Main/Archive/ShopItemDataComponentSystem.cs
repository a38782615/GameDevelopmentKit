using System.Collections.Generic;

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
            self.EnsureItems();
            self.Items.Clear();

            IReadOnlyList<DRItems> itemConfigs = Tables.Instance?.DTItems?.DataList;
            if (itemConfigs == null)
            {
                Log.Warning("Shop construction failed: DTItems is not loaded.");
                return;
            }

            foreach (DRItems itemConfig in itemConfigs)
            {
                if (itemType > 0 && itemConfig.ItemType != itemType)
                {
                    continue;
                }

                self.Items.Add(new ShopItemData
                {
                    Id = IdGenerater.Instance.GenerateId(),
                    ConfigId = itemConfig.Id,
                    Count = GetDefaultCount(itemConfig.ItemType),
                });
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
            if (inventoryDataComponent == null || !self.CanBuy(item, playerData))
            {
                return false;
            }

            DRItems itemConfig = Tables.Instance.DTItems.Get(item.ConfigId);
            InventoryItemData inventoryItem = inventoryDataComponent.AddItem(item.ConfigId);
            if (inventoryItem == null)
            {
                return false;
            }

            playerData.Diamond -= itemConfig.Diamond;
            item.Count--;
            return true;
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
