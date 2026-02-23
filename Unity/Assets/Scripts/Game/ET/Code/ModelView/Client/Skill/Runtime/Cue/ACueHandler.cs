namespace ET.Client
{
    /// <summary>
    /// 条件Handler标记特性 - 用于自动收集条件Handler
    /// </summary>
    public class CueHandlerAttribute : BaseAttribute
    {
    }
    /// <summary>
    /// 条件Handler抽象基类 - 子类实现具体的条件判断逻辑
    /// 参考 AAIHandler 模式
    /// </summary>
    [CueHandler]
    public abstract class ACueHandler : HandlerObject
    {
        public GameplayCueSpec Spec;
        public CueNodeData NodeData;
        /// <summary>
        /// 播放Cue（子类必须实现）
        /// </summary>
        public abstract void PlayCue(AbilitySystemComponent target);

        /// <summary>
        /// 停止Cue（子类必须实现）
        /// </summary>
        public abstract void StopCue();

        public abstract void OnInitialize();

        public abstract void Reset();

        public abstract SpecExecutionContext GetContext();
    }
}
