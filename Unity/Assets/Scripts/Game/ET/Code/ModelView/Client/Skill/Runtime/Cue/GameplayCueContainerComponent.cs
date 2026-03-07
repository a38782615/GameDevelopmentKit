using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// Cue容器组件 - 统一管理ASC上所有运行中的Cue
    /// </summary>
    [ComponentOf(typeof(AbilitySystemComponent))]
    public class GameplayCueContainerComponent : Entity, IAwake, IUpdate, IDestroy
    {
        /// <summary>
        /// 运行中的Cue列表
        /// </summary>
        public List<EntityRef<GameplayCueSpec>> ActiveCues = new List<EntityRef<GameplayCueSpec>>();

        /// <summary>
        /// 待移除的Cue（避免遍历时修改）
        /// </summary>
        public List<EntityRef<GameplayCueSpec>> PendingRemove = new List<EntityRef<GameplayCueSpec>>();

        /// <summary>
        /// 是否正在更新
        /// </summary>
        public bool IsUpdating;

        /// <summary>
        /// 运行中Cue数量
        /// </summary>
        public int Count => ActiveCues.Count;

        // ============ 便捷访问 ============

        /// <summary>
        /// 获取所属ASC
        /// </summary>
        public AbilitySystemComponent GetASC => this.GetParent<AbilitySystemComponent>();
    }
}
