namespace ET.Client
{
    /// <summary>
    /// Buff效果Spec（持续效果）
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]
    public class BuffEffectSpecHandler : AEffectHandler
    {
        public BuffEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<BuffEffectSpec>();
            return selfSpec;
        }

        public BuffEffectNodeData GetNode()
        {
            var nodeData = NodeData as BuffEffectNodeData;
            return nodeData;
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
            executionContext.ProjectileObject = context.ProjectileObject;
            executionContext.PlacementObject = context.PlacementObject;
            executionContext.AbilityLevel = context.AbilityLevel;
            executionContext.StackCount = Spec.StackCount;
            executionContext.Targets.AddRange(context.Targets);

            foreach (var kvp in context.CustomData)
            {
                executionContext.CustomData[kvp.Key] = kvp.Value;
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
