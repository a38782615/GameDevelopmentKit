using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 时间Cue运行时数据 - 有开始和结束时间，管理Cue生命周期
    /// </summary>
    public class TimeCueRuntime : Object
    {
        public float StartTime { get; set; }
        public float EndTime { get; set; }
        public string PortId { get; set; }
        public bool HasStarted { get; set; }
        public bool HasEnded { get; set; }
        /// <summary>
        /// 触发的Cue Spec列表（用于生命周期管理）
        /// </summary>
        public List<GameplayCueSpec> TriggeredCueSpecs { get; set; } = new List<GameplayCueSpec>();
    }
}
