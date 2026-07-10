using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class InventoryDataComponent : Entity, IAwake, IDestroy
    {
        public XList<InventoryItemData> BagItems = new XList<InventoryItemData>();
        public XList<InventoryItemData> Items = new XList<InventoryItemData>();
        public XList<InventoryItemData> Drops = new XList<InventoryItemData>();
        public XDictionary<int, long> SlotToItemId = new XDictionary<int, long>();
    }
}
