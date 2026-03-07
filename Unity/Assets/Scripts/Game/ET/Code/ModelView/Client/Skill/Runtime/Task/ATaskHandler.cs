namespace ET.Client
{
    public class TaskHandlerAttribute : BaseAttribute
    {
    }

    [TaskHandler]
    public abstract class ATaskHandler : HandlerObject
    {
        public TaskSpec Spec;
        public TaskNodeData NodeData;

        public abstract void Execute();

        public abstract SpecExecutionContext GetContext();
    }
}
