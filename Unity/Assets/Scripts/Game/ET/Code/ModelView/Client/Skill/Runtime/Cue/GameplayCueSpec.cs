namespace ET.Client
{
    /// <summary>
    /// Cue Spec 基类，负责承载节点配置和播放时的上下文。
    /// </summary>
    [ChildOf(typeof(GameplayCueContainerComponent))]
    public class GameplayCueSpec : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 技能 ID，用于从数据中心获取节点数据。
        /// </summary>
        public string SkillId;

        /// <summary>
        /// 节点 Guid。
        /// </summary>
        public string NodeGuid;

        /// <summary>
        /// 执行上下文所属的 AbilitySpec。
        /// </summary>
        public EntityRef<GameplayAbilitySpec> ContextOwner;

        /// <summary>
        /// 实际触发该 Cue 的执行上下文。
        /// </summary>
        public EntityRef<SpecExecutionContext> Context;

        /// <summary>
        /// 是否正在播放。
        /// </summary>
        public bool IsRunning;

        /// <summary>
        /// 是否已被取消。
        /// </summary>
        public bool IsCancelled;

        /// <summary>
        /// 是否随节点销毁。
        /// </summary>
        public bool DestroyWithNode;

        /// <summary>
        /// 标签容器。
        /// </summary>
        public CueTagContainer Tags;

        /// <summary>
        /// 当前激活的运行态组件。
        /// </summary>
        public EntityRef<ActiveCueComponent> ActiveCueComponent;

        /// <summary>
        /// 获取节点数据。
        /// </summary>
        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(SkillId, NodeGuid);

        /// <summary>
        /// 获取 Cue 节点数据。
        /// </summary>
        public CueNodeData CueNodeData => NodeData as CueNodeData;

        public string HandName;
    }
}
