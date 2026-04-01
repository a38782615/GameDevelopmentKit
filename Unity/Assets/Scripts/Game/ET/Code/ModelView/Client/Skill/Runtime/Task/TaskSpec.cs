namespace ET.Client
{
    [ChildOf(typeof(AbilitySystemComponent))]
    public class TaskSpec : Entity, IAwake, IDestroy
    {
        public string SkillId;
        public string NodeGuid;
        public EntityRef<SpecExecutionContext> Context;
        public EntityRef<AbilitySystemComponent> Source;
        public string HandName;
    }
}
