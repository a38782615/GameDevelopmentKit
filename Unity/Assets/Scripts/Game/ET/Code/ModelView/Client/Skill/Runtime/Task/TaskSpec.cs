using System;


namespace ET.Client
{
    /// <summary>
    /// 任务Spec基类 - 用于执行特定任务的节点
    /// 特点：瞬时执行、无属性修改、无堆叠、无持续时间
    /// </summary>
    [ComponentOf(typeof(AbilitySystemComponent))]
    public partial class TaskSpec : Entity, IAwake, IDestroy
    {
        // ============ 基础标识 ============
        public string SpecId;
        public string SkillId;
        public string NodeGuid;
        public SpecExecutionContext Context;
        public EntityRef<AbilitySystemComponent> Source;

        // ============ 静态数据访问 ============
        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(SkillId, NodeGuid);
        public TaskNodeData TaskNodeData => NodeData as TaskNodeData;

    }
}
