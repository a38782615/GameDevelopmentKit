using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormShop : UGFUIForm<MonoUIFormShop>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public readonly List<ShopItemData> DisplayItems = new List<ShopItemData>();
        public readonly Dictionary<int, EntityRef<UIWidgetShopItem>> ItemWidgets = new Dictionary<int, EntityRef<UIWidgetShopItem>>();
    }
}
