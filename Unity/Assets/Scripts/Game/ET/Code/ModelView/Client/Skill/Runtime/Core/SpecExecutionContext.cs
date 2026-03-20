using System.Collections.Generic;
using UnityEngine;


namespace ET.Client
{
    /// <summary>
    /// Spec执行上下文 - 提供执行所需的所有信息
    /// 在整个技能执行过程中传递
    /// </summary>
    [ChildOf(typeof(GameplayAbilitySpec))]
    public partial class SpecExecutionContext : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 当前触发执行的EffectSpec Entity Id（用于管理Cue生命周期，如Buff）
        /// </summary>
        public EntityRef<GameplayEffectSpec> OwnerEffectSpec;

        /// <summary>
        /// 施法者 ASC Entity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> Caster;

        /// <summary>
        /// 当前目标列表（ASC Entity Id）
        /// </summary>
        public List<EntityRef<AbilitySystemComponent>> Targets = new List<EntityRef<AbilitySystemComponent>>();

        /// <summary>
        /// 技能主目标 ASC Entity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> MainTarget;

        /// <summary>
        /// 父节点传入的目标 ASC Entity Id
        /// </summary>
        public EntityRef<AbilitySystemComponent> ParentInputTarget;

        /// <summary>
        /// 投射物对象（View层引用）
        /// </summary>
        public GameObject ProjectileObject;

        /// <summary>
        /// 放置物对象（View层引用）
        /// </summary>
        public GameObject PlacementObject;

        /// <summary>
        /// 技能等级
        /// </summary>
        public int AbilityLevel = 1;

        /// <summary>
        /// 堆叠层数
        /// </summary>
        public int StackCount = 1;

        /// <summary>
        /// 自定义数据字典（SetByCaller数据）
        /// </summary>
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
    }
}
