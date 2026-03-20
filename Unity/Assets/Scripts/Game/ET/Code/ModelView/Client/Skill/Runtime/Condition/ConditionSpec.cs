namespace ET.Client
{
    [ChildOf(typeof(AbilitySystemComponent))]
    public class ConditionSpec : Entity, IAwake, IDestroy
    {
        public string SkillId;
        public string NodeGuid;
        public EntityRef<SpecExecutionContext> Context;
        public EntityRef<AbilitySystemComponent> Source;
        public string HandName;

        public ConditionNodeData ConditionNodeData => NodeData as ConditionNodeData;

        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(this.SkillId, this.NodeGuid);
    }
}
