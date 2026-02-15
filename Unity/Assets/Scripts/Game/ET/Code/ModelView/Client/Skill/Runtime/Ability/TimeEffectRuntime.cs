namespace ET.Client
{
    /// <summary>
    /// 时间效果运行时数据
    /// </summary>
    public class TimeEffectRuntime : Object
    {
        public float TriggerTime { get; set; }
        public string PortId { get; set; }
        public bool HasTriggered { get; set; }
    }
}
