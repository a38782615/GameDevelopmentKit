using System;


namespace ET.Client
{
    public class TaskSpecHandlerAttribute : BaseAttribute
    {
    }

    [TaskSpecHandler]
    public abstract class ATaskSpecHandler : HandlerObject
    {
        public TaskSpec Spec;
        public TaskNodeData NodeData;
        public abstract SpecExecutionContext GetExecutionContext();
        public abstract void OnInitialize();
        public abstract void Execute();
        public abstract SpecExecutionContext GetContext();
        public abstract void OnInitialHook(AbilitySystemComponent target);
        public abstract void OnPeriodicHook();
        public abstract void OnCompleteHook();
    }
}