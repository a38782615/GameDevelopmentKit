using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Cue Spec基类 - 包含动态数据和执行逻辑
    /// Cue节点用于播放视觉/音效表现，不改变游戏状态
    /// </summary>
    [ChildOf(typeof(GameplayCueContainerComponent))]
    public class GameplayCueSpec : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 技能ID（用于从数据中心获取数据）
        /// </summary>
        public string SkillId;

        /// <summary>
        /// 节点Guid
        /// </summary>
        public string NodeGuid;

        /// <summary>
        /// 执行上下文所属的 AbilitySpec Entity Id
        /// </summary>
        public EntityRef<GameplayAbilitySpec> ContextOwner;

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsRunning;

        /// <summary>
        /// 是否已取消
        /// </summary>
        public bool IsCancelled;

        /// <summary>
        /// 随节点销毁
        /// </summary>
        public bool DestroyWithNode;

        // ============ 动态数据 ============

        /// <summary>
        /// 标签容器
        /// </summary>
        public CueTagContainer Tags;

        /// <summary>
        /// 激活的Cue实例
        /// </summary>
        public ActiveGameplayCue ActiveCue;

        // ============ 静态数据访问 ============

        /// <summary>
        /// 获取节点数据（从数据中心）
        /// </summary>
        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(SkillId, NodeGuid);

        /// <summary>
        /// 获取Cue节点数据
        /// </summary>
        public CueNodeData CueNodeData => NodeData as CueNodeData;
        public string HandName;
    }
}
