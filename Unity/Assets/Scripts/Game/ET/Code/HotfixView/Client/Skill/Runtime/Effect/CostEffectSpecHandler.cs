

namespace ET.Client
{
    /// <summary>
    /// 消耗效果Spec（瞬时效果）
    /// </summary>namespace ET.Client
    public partial class CostEffectSpecHandler : AEffectHandler
    {
        public CostEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<CostEffectSpec>();
            return selfSpec;
        }
        public CostEffectNodeData GetNode()
        {
            var nodeData = NodeData as CostEffectNodeData;
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
