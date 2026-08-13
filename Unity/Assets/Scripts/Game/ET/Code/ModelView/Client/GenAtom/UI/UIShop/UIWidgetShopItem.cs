namespace ET.Client
{
    [ComponentOf(typeof(UIFormShop))]
    public class UIWidgetShopItem : UGFUIWidget<MonoUIShopItem>, IAwake, IUGFUIWidgetOnOpen
    {
        public ShopItemData Data;
    }
}
