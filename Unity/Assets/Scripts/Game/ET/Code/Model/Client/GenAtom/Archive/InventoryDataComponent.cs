namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class InventoryDataComponent : Entity, IAwake, IDestroy
    {
        public InventoryData InventoryData;
    }
}
