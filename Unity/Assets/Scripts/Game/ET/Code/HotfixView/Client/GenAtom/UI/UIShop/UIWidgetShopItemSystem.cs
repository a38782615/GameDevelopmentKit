namespace ET.Client
{
    [EntitySystemOf(typeof(UIWidgetShopItem))]
    [FriendOf(typeof(UIWidgetShopItem))]
    [FriendOf(typeof(UIFormShop))]
    public static partial class UIWidgetShopItemSystem
    {
        [EntitySystem]
        private static void Awake(this UIWidgetShopItem self)
        {
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetShopItem self)
        {
            UIFormShop shop = self.GetParent<UIFormShop>();
            if (shop == null || self.CachedRectTransform == null)
            {
                return;
            }

            shop.ItemWidgets[self.CachedRectTransform.gameObject.GetInstanceID()] = self;
        }

        public static void Bind(this UIWidgetShopItem self, ShopItemData itemData)
        {
            self.Data = itemData;
            DRItems itemConfig = itemData == null ? null : Tables.Instance.DTItems.GetOrDefault(itemData.ConfigId);

            self.View.NameUXTextMeshPro.text = itemConfig?.Name ?? string.Empty;
            self.View.CountUXTextMeshPro.text = itemData == null ? string.Empty : itemData.Count.ToString();
            self.View.IconImage.enabled = itemData != null;
        }
    }
}
