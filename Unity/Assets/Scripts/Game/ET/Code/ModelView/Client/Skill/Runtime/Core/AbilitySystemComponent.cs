using System;

namespace ET.Client
{
    /// <summary>
    /// 技能系统组件 - GAS的核心实现
    /// 管理技能、效果、属性、标签的中枢组件
    /// </summary>
    [ComponentOf(typeof(SkillUnit))]
    public partial class AbilitySystemComponent : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 属性容器
        /// </summary>
        public global::ET.AttributeComponent AttributeComponent => this.GetParent<SkillUnit>()?.Unit.As()?.GetComponent<global::ET.AttributeComponent>();

        public AttributeSetContainer Attributes => this.AttributeComponent?.RuntimeAttributes as AttributeSetContainer;

        /// <summary>
        /// 标签容器
        /// </summary>
        public GameplayTagContainer OwnedTags;

        /// <summary>
        /// 所属的GameObject（View层引用）
        /// </summary>
        public UnityEngine.GameObject Owner;

        /// <summary>
        /// 是否已初始化
        /// </summary>
        public bool IsInitialized;

        // ============ 事件 ============

        public struct OnAbilityActivated
        {
            public GameplayAbilitySpec Spec;
            public OnAbilityActivated(GameplayAbilitySpec spec)
            {
                Spec = spec;
            }

        }
        public struct OnAbilityEnded
        {
            public GameplayAbilitySpec Spec;
            public bool End;
            public OnAbilityEnded(GameplayAbilitySpec spec, bool end)
            {
                this.Spec = spec;
                this.End = end;
            }
        }
        public struct OnEffectApplied
        {
            public GameplayEffectSpec Spec;
            public AbilitySystemComponent Abi;
            public OnEffectApplied(GameplayEffectSpec spec, AbilitySystemComponent abi)
            {
                this.Spec = spec;
                this.Abi = abi;
            }
        }
        public struct OnEffectRemoved
        {
            public GameplayEffectSpec Spec;
            public OnEffectRemoved(GameplayEffectSpec spec)
            {
                this.Spec = spec;
            }
        }
        public struct OnTagChanged
        {
            public GameplayTag Tag;
            public bool Change;
            public OnTagChanged(GameplayTag tag, bool change)
            {
                this.Tag = tag;
                this.Change = change;
            }
        }
        public struct OnTGameplayEvent
        {
            public GameplayEventType GameplayEventType;
            public OnTGameplayEvent(GameplayEventType gameplayEventType)
            {
                this.GameplayEventType = gameplayEventType;
            }
        }

        // ============ 便捷访问 ============

        /// <summary>
        /// 技能容器
        /// </summary>
        public AbilityContainerComponent Abilities => this.GetComponent<AbilityContainerComponent>();

        /// <summary>
        /// 效果容器
        /// </summary>
        public GameplayEffectContainerComponent EffectContainer => this.GetComponent<GameplayEffectContainerComponent>();
    }
}
