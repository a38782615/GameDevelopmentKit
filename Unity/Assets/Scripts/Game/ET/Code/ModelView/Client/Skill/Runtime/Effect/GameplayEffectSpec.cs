using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 效果Spec基类
    /// 瞬时效果: 不授予标签，直接修改BaseValue（永久）
    /// 持续效果: Apply时授予标签+添加Modifier（临时），Remove时移除
    /// </summary>
    [ChildOf(typeof(GameplayEffectContainerComponent))]
    public class GameplayEffectSpec : Entity, IAwake, IUpdate, IDestroy
    {
        // ============ 基础标识 ============
        public string SkillId;
        public string NodeGuid;
        public SpecExecutionContext Context;
        public EntityRef<AbilitySystemComponent> Source;
        public EntityRef<AbilitySystemComponent> Target;
        public int Level = 1;
        public int StackCount = 1;
        public bool IsRunning;
        public bool IsCancelled;

        // ============ 运行时数据（可被修改） ============
        public EffectTagContainer Tags;
        public float Duration;
        public float Period;
        public List<AttributeModifier> Modifiers = new List<AttributeModifier>();
        public Dictionary<string, float> SetByCallerValues = new Dictionary<string, float>();
        public Dictionary<AttrType, float> SnapshotValues = new Dictionary<AttrType, float>();

        // ============ 运行时状态 ============
        public float ActivationTime;
        public bool IsApplied;
        public bool IsExpired;
        public bool WasRefreshed;
        public float ElapsedTime;
        public float PeriodTimer;
        public List<long> TriggeredCueIds = new List<long>(); // GameplayCueSpec Entity Ids

        // ============ 静态数据访问 ============
        public NodeData NodeData => SkillDataCenter.Instance.GetNodeData(SkillId, NodeGuid);
        public EffectNodeData EffectNodeData => NodeData as EffectNodeData;

        public float RemainingTime => EffectNodeData?.durationType == EffectDurationType.Duration
            ? Math.Max(0f, Duration - ElapsedTime)
            : -1f;

        public string HandName;
    }
}
