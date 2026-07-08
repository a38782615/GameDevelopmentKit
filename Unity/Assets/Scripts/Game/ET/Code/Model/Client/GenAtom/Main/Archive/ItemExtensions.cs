namespace ET.Client
{
    public static class ItemExtensions
    {
        public static DRItems GetItemConfig(this int itemId)
        {
            return Tables.Instance.DTItems.GetOrDefault(itemId);
        }

        public static bool IsEquipItem(this int itemId)
        {
            DRItems itemConfig = itemId.GetItemConfig();
            return itemConfig != null && itemConfig.ItemType == 2;
        }
    }
}
