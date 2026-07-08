using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class InventoryDataComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, InventoryItemData> Items = new Dictionary<long, InventoryItemData>();
        public Dictionary<long, InventoryItemData> Drops = new Dictionary<long, InventoryItemData>();
        public Dictionary<int, long> SlotToItemId = new Dictionary<int, long>();
    }
}
