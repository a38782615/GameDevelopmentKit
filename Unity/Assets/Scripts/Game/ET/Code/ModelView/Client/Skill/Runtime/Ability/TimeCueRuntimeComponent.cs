using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 时间Cue运行时组件 - 管理技能的所有时间Cue数据
    /// TimeCueRuntime 保持为普通数据类
    /// </summary>
    [ComponentOf(typeof(GameplayAbilitySpec))]
    public class TimeCueRuntimeComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// 时间Cue列表
        /// </summary>
        public List<TimeCueRuntime> TimeCues = new List<TimeCueRuntime>();
    }
}
