using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(UIComponent))]
    public class UIFormShop : UGFUIForm<MonoUIFormShop>, IAwake, IDestroy, IUGFUIFormOnOpen, IUGFUIFormOnClose
    {
        public readonly List<ShopItemData> DisplayItems = new List<ShopItemData>();
    }
}
