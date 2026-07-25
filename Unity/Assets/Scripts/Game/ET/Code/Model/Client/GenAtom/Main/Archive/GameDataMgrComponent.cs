namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class GameDataMgrComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<PlayerDataComponent> PlayerDataComponent;
        public EntityRef<PlayerSkillDataComponent> PlayerSkillDataComponent;
        public EntityRef<TaskDataComponent> TaskDataComponent;
        public EntityRef<InventoryDataComponent> InventoryDataComponent;
    }
}
