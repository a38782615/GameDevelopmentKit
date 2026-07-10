namespace ET.Client
{
    public partial class InventoryItemData : Object
    {
        public long Id;
        public int ConfigId;
        public int Count;
        public bool IsEquipped;
        public int EquipSlot;
        public int Type1;// 0 储物袋 1 仓库

    }
}
