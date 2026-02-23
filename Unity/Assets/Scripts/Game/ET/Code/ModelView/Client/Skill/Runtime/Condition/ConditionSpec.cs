namespace ET.Client
{
    /// <summary>
    /// 条件Spec - 条件判断节点的运行时Entity
    /// 瞬时创建，执行后Dispose
    /// </summary>
    [ChildOf(typeof(AbilitySystemComponent))]
    public class ConditionSpec : Entity, IAwake, IDestroy
    {
        // ============ 基础标识 ============
        public string SkillId;
        public string NodeGuid;

        /// <summary>
        /// 执行上下文所属的 AbilitySpec Entity Id
        /// </summary>
        public SpecExecutionContext Context;

        /// <summary>
        /// 施法者 ASC Entity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> Source;

        // ============ 静态数据访问 ============
        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(SkillId, NodeGuid);
        public ConditionNodeData ConditionNodeData => NodeData as ConditionNodeData;
    }
}
