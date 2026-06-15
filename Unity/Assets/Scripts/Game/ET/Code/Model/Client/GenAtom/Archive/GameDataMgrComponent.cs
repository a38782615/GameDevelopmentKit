namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class GameDataMgrComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<PlayerDataComponent> PlayerDataComponent;
        public EntityRef<TaskDataComponent> TaskDataComponent;
    }
}
