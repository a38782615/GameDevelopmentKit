namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class ShopItemDataComponent : Entity, IAwake, IDestroy
    {
        public XList<ShopItemData> Items;
    }
}
