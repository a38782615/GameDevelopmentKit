using System;
using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 技能容器组件 - 管理ASC拥有的所有技能
    /// </summary>
    [ComponentOf(typeof(AbilitySystemComponent))]
    public partial class AbilityContainerComponent : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 已授予的技能列表 GameplayAbilitySpec
        /// </summary>
        public List<EntityRef<GameplayAbilitySpec>> GrantedAbilities = new List<EntityRef<GameplayAbilitySpec>>();

        /// <summary>
        /// 正在激活的技能列表
        /// </summary>
        public List<EntityRef<GameplayAbilitySpec>> ActiveAbilities = new List<EntityRef<GameplayAbilitySpec>>();

        /// <summary>
        /// 待移除的技能（避免遍历时修改）
        /// </summary>
        public List<EntityRef<GameplayAbilitySpec>> PendingRemove = new List<EntityRef<GameplayAbilitySpec>>();

        /// <summary>
        /// 是否正在更新
        /// </summary>
        public bool IsUpdating;

        // ============ 事件 ============
        public struct OnAbilityGranted
        {
            public GameplayAbilitySpec Spec
            {
                private set;
                get;
            }
            public OnAbilityGranted(GameplayAbilitySpec spec)
            {
                Spec = spec;
            }
        }
        public struct OnAbilityRemoved
        {
            public GameplayAbilitySpec Spec
            {
                private set;
                get;
            }
            public OnAbilityRemoved(GameplayAbilitySpec spec)
            {
                Spec = spec;
            }
        }
        // ============ 便捷访问 ============

        /// <summary>
        /// 已授予技能数量
        /// </summary>
        public int GrantedCount => GrantedAbilities.Count;

        /// <summary>
        /// 激活中技能数量
        /// </summary>
        public int ActiveCount => ActiveAbilities.Count;

        /// <summary>
        /// 获取所属ASC
        /// </summary>
        public AbilitySystemComponent GetASC => this.GetParent<AbilitySystemComponent>();
    }
}
