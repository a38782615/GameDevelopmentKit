namespace ET.Client
{
    public partial class GenericEffectSpecHandler : AEffectHandler
    {
        public GenericEffectSpec SelfSpec()
        {
            return Spec.GetComponent<GenericEffectSpec>();
        }

        public GenericEffectNodeData GetNode()
        {
            return NodeData as GenericEffectNodeData;
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
