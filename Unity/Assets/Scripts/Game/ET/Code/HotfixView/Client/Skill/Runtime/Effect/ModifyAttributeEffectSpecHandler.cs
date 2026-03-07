

namespace ET.Client
{
    /// <summary>
    /// 属性修改效果Spec（瞬时效果）
    /// 完全依赖基类处理，无需额外逻辑
    /// </summary>
    public partial class ModifyAttributeEffectSpecHandler : AEffectHandler
    {
        public ModifyAttributeEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<ModifyAttributeEffectSpec>();
            return selfSpec;
        }
        public ModifyAttributeEffectNodeData GetNode()
        {
            var nodeData = NodeData as ModifyAttributeEffectNodeData;
            return nodeData;
        }
        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }
        public override void Cancel()
        {
            Spec.CancelEffect();
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void OnCompleteHook()
        {
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
        }

        public override void OnInitialize()
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void Reset()
        {
            Spec.ResetEffect();
        }

        public override void Tick(float deltaTime)
        {
            Spec.TickEffect(deltaTime);
        }
    }
}
