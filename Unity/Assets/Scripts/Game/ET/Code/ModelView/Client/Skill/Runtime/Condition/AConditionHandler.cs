namespace ET.Client
{
    /// <summary>
    /// 条件Handler标记特性 - 用于自动收集条件Handler
    /// </summary>
    public class ConditionHandlerAttribute : BaseAttribute
    {
    }
    /// <summary>
    /// 条件Handler抽象基类 - 子类实现具体的条件判断逻辑
    /// 参考 AAIHandler 模式
    /// </summary>
    [ConditionHandler]
    public abstract class AConditionHandler : HandlerObject
    {
        public ConditionSpec ConditionSpec;
        /// <summary>
        /// 执行条件判断
        /// </summary>
        /// <param name="conditionSpec">条件Spec Entity</param>
        /// <param name="target">目标ASC</param>
        /// <returns>条件是否满足</returns>
        public abstract bool Evaluate(AbilitySystemComponent target);
    }
}
