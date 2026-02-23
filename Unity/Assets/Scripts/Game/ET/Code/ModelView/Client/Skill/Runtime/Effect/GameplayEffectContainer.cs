using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 效果容器组件 - 管理ASC上所有激活的效果
    /// </summary>
    [ComponentOf(typeof(AbilitySystemComponent))]
    public class GameplayEffectContainerComponent : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 激活的效果列表
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> ActiveEffects = new List<EntityRef<GameplayEffectSpec>>();

        /// <summary>
        /// 待移除的效果（避免遍历时修改）
        /// </summary>
        public List<EntityRef<GameplayEffectSpec>> PendingRemove = new List<EntityRef<GameplayEffectSpec>>();

        /// <summary>
        /// 是否正在更新
        /// </summary>
        public bool IsUpdating;

        /// <summary>
        /// 激活效果数量
        /// </summary>
        public int Count => ActiveEffects.Count;

        // ============ 便捷访问 ============

        /// <summary>
        /// 获取所属ASC
        /// </summary>
        public AbilitySystemComponent GetASC => this.GetParent<AbilitySystemComponent>();
    }
}
