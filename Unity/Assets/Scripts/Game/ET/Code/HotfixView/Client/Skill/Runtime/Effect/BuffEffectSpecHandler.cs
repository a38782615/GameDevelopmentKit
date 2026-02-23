using System.Collections.Generic;


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
            var Context = GetContext();
            if (Context == null)
            {
                return null;
            }
            // Buff 的目标就是 Target（Buff 持有者）
            var currentTarget = Spec.Target.As() ?? Context.GetTargetByType(NodeData?.targetType ?? TargetType.ParentInput);

            return new SpecExecutionContext
            {
                AbilitySpec = Context.AbilitySpec,
                OwnerEffectSpec = Spec,
                Caster = Context.Caster,
                MainTarget = Context.MainTarget,
                ParentInputTarget = currentTarget,  // 将 Buff 的目标作为 ParentInputTarget 传递
                AbilityLevel = Context?.AbilityLevel ?? 1,
                StackCount = Spec.StackCount  // 传递 Buff 的堆叠层数
            };
        }
        public override void OnInitialize()
        {
            Spec.OnInitialize();
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
