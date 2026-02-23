namespace ET.Client
{
    /// <summary>
    /// 通用效果Spec - 完全依赖基类处理
    /// </summary>
    public partial class GenericEffectSpecHandler : AEffectHandler
    {
        public GenericEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<GenericEffectSpec>();
            return selfSpec;
        }
        public GenericEffectNodeData GetNode()
        {
            var nodeData = NodeData as GenericEffectNodeData;
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
