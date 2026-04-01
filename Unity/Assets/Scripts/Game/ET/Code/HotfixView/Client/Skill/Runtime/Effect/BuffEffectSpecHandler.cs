namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]
    public class BuffEffectSpecHandler : AEffectHandler
    {
        public BuffEffectSpec SelfSpec()
        {
            return Spec.GetComponent<BuffEffectSpec>();
        }

        public BuffEffectNodeData GetNode()
        {
            return NodeData as BuffEffectNodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            SpecExecutionContext context = GetContext();
            if (context == null)
            {
                return null;
            }

            GameplayAbilitySpec abilitySpec = context.GetAbilitySpec();
            if (abilitySpec == null)
            {
                return null;
            }

            AbilitySystemComponent currentTarget = Spec.Target.As() ?? context.GetTargetByType(NodeData?.targetType ?? TargetType.ParentInput);
            SpecExecutionContext executionContext = abilitySpec.AddChild<SpecExecutionContext>();
            executionContext.OwnerEffectSpec = Spec;
            executionContext.Caster = context.Caster;
            executionContext.MainTarget = context.MainTarget;
            executionContext.ParentInputTarget = currentTarget;
            executionContext.AbilityLevel = context.AbilityLevel;
            executionContext.StackCount = Spec.StackCount;
            executionContext.Targets.AddRange(context.Targets);

            foreach ((string key, object value) in context.CustomData)
            {
                executionContext.CustomData[key] = value;
            }

            return executionContext;
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override void Tick(float deltaTime)
        {
            Spec.TickEffect(deltaTime);
        }

        public override void Cancel()
        {
            Spec.CancelEffect();
        }

        public override void Reset()
        {
            Spec.ResetEffect();
        }

        public override void OnInitialize()
        {
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void OnCompleteHook()
        {
        }
    }
}
