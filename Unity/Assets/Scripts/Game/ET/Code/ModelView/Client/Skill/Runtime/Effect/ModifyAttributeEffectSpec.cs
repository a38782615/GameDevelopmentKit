namespace ET.Client
{
    /// <summary>
    /// 属性修改效果Spec（瞬时效果）
    /// 完全依赖基类处理，无需额外逻辑
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ModifyAttributeEffectSpec : Entity, IAwake
    {
    }
}
