using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class InventoryDataComponent : Entity, IAwake, IDestroy
    {
        public XDictionary<long, InventoryItemData> Items = new XDictionary<long, InventoryItemData>();
        public XDictionary<long, InventoryItemData> Drops = new XDictionary<long, InventoryItemData>();
        public XDictionary<int, long> SlotToItemId = new XDictionary<int, long>();
    }
}
