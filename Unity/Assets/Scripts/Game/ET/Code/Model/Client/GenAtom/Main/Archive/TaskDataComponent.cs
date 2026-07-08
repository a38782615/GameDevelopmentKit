namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class TaskDataComponent : Entity, IAwake, IDestroy
    {
        public TaskData TaskData;
    }
}
