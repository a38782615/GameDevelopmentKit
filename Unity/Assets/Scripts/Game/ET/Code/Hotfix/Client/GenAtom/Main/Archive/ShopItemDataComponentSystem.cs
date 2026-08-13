using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(ShopItemDataComponent))]
    [FriendOf(typeof(ShopItemDataComponent))]
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
