using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 时间效果运行时组件 - 管理技能的所有时间效果数据
    /// TimeEffectRuntime 保持为普通数据类
    /// </summary>
    [ComponentOf(typeof(GameplayAbilitySpec))]
    public class TimeEffectRuntimeComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 时间效果列表
        /// </summary>
        public List<TimeEffectRuntime> TimeEffects = new List<TimeEffectRuntime>();
    }
}
