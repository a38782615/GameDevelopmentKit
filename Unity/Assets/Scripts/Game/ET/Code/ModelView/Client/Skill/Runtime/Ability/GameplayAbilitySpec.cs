using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 技能运行时实例 - 对应GAS的FGameplayAbilitySpec
    /// 每个授予的技能都有一个Spec实例，包含运行时状态和执行逻辑
    /// </summary>
    [ChildOf(typeof(AbilityContainerComponent))]
    public partial class GameplayAbilitySpec : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 技能ID（用于从数据中心获取数据）
        /// </summary>
        public string SkillId;

        /// <summary>
        /// 拥有此技能的ASC的Entity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> Owner;

        /// <summary>
        /// 当前状态
        /// </summary>
        public AbilityState State = AbilityState.Inactive;

        /// <summary>
        /// 技能等级
        /// </summary>
        public int Level = 1;

        /// <summary>
        /// 标签容器
        /// </summary>
        public AbilityTagContainer Tags;

        /// <summary>
        /// 激活时间
        /// </summary>
        public float ActivationTime;

        /// <summary>
        /// 是否正在激活
        /// </summary>
        public bool IsActive => State == AbilityState.Active;

        /// <summary>
        /// 是否正在执行
        /// </summary>
        public bool IsRunning;

        // ============ 静态数据访问 ============

        /// <summary>
        /// 技能图表数据（从数据中心获取）
        /// </summary>
        public SkillData GraphData => SkillDataCenter.Instance.GetSkillGraph(SkillId);

        /// <summary>
        /// 技能节点数据
        /// </summary>
        public AbilityNodeData AbilityNodeData;

        /// <summary>
        /// Ability节点的guid
        /// </summary>
        public string AbilityNodeGuid;

        // ============ 执行相关 ============
        public EntityRef<SpecExecutionContext> Context;

        /// <summary>
        /// 正在执行的Effect列表（技能管理持续/周期Effect）
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> RunningEffects = new List<EntityRef<GameplayEffectSpec>>();

        /// <summary>
        /// 待移除的Effect
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> PendingRemoveEffects = new List<EntityRef<GameplayEffectSpec>>();

        // ============ 缓存的节点数据 ============

        /// <summary>
        /// 消耗节点Guid
        /// </summary>
        public string CostNodeGuid;

        /// <summary>
        /// 冷却节点Guid
        /// </summary>
        public string CooldownNodeGuid;

        /// <summary>
        /// 动画节点Guid
        /// </summary>
        public string AnimationNodeGuid;

        // ============ 动画相关 ============

        /// <summary>
        /// 动画名称
        /// </summary>
        public string AnimationName;

        /// <summary>
        /// 动画时长
        /// </summary>
        public float AnimationDuration;

        /// <summary>
        /// 是否循环播放动画
        /// </summary>
        public bool IsAnimationLooping;

        /// <summary>
        /// 当前播放时间
        /// </summary>
        public float CurrentPlayTime;

        // ============ 事件 ============

        public struct OnActivated
        {
            public GameplayAbilitySpec Spec;

        }
        public struct OnEnded
        {
            public GameplayAbilitySpec Spec;
            public bool End;
        }

        // ============ 便捷访问 ============

        /// <summary>
        /// 获取所属ASC
        /// </summary>
        public AbilitySystemComponent GetASC => this.GetParent<AbilityContainerComponent>()?.GetASC;

        /// <summary>
        /// 获取时间Cue运行时组件
        /// </summary>
        public TimeCueRuntimeComponent GetTimeCueRuntime => this.GetComponent<TimeCueRuntimeComponent>();

        /// <summary>
        /// 获取时间效果运行时组件
        /// </summary>
        public TimeEffectRuntimeComponent GetTimeEffectRuntime => this.GetComponent<TimeEffectRuntimeComponent>();
    }
}
