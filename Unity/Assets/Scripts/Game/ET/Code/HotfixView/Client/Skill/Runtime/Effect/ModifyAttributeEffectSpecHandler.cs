namespace ET.Client
{
    public partial class ModifyAttributeEffectSpecHandler : AEffectHandler
    {
        public ModifyAttributeEffectSpec SelfSpec()
        {
            return Spec.GetComponent<ModifyAttributeEffectSpec>();
        }

        public ModifyAttributeEffectNodeData GetNode()
        {
            return NodeData as ModifyAttributeEffectNodeData;
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
