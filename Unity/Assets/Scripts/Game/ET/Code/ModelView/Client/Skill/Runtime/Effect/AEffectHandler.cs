namespace ET.Client
{
    /// <summary>
    /// 条件Handler标记特性 - 用于自动收集条件Handler
    /// </summary>
    public class EffectHandlerAttribute : BaseAttribute
    {
    }
    /// <summary>
    /// 条件Handler抽象基类 - 子类实现具体的条件判断逻辑
    /// 参考 AAIHandler 模式
    /// </summary>
    [EffectHandler]
    public abstract class AEffectHandler : HandlerObject
    {
        public GameplayEffectSpec Spec;
        public EffectNodeData NodeData;
        public abstract SpecExecutionContext GetExecutionContext();
        public abstract void OnInitialize();
        public abstract void Execute();
        public abstract SpecExecutionContext GetContext();
        // ============ 三个钩子（子类可重写） ============
        public abstract void OnInitialHook(AbilitySystemComponent target);
        public abstract void OnPeriodicHook();
        public abstract void OnCompleteHook();

        public abstract void Tick(float deltaTime);
        public abstract void Cancel();
        public abstract void Reset();
    }
}
