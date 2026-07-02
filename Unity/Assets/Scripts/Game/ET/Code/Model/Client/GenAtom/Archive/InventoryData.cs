using System.Collections.Generic;

namespace ET.Client
{
    public partial class InventoryData : Object
    {
        public InventoryBagData BagData = new InventoryBagData();
        public InventoryEquipData EquipData = new InventoryEquipData();
    }

    public partial class InventoryBagData : Object
    {
        public Dictionary<long, InventoryItemData> Items = new Dictionary<long, InventoryItemData>();
    }

    public partial class InventoryEquipData : Object
    {
        public Dictionary<int, long> SlotToItemId = new Dictionary<int, long>();
    }

    public partial class InventoryItemData : Object
    {
        public long Id;
        public int ConfigId;
        public int Count;
        public bool IsEquipped;
        public int EquipSlot;
    }
}
