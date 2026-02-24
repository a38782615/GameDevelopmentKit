

namespace ET.Client
{
    /// <summary>
    /// 结束技能任务Spec
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.TaskSpec))]
    [FriendOfAttribute(typeof(ET.Client.EndAbilityTaskSpec))]
    public class EndAbilityTaskSpecHandler : ATaskSpecHandler
    {
        private EndAbilityTaskNodeData GetNode()
        {
            return NodeData as EndAbilityTaskNodeData;
        }

        public EndAbilityTaskSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<EndAbilityTaskSpec>();
            if (selfSpec == null)
            {
                Spec.AddComponent<EndAbilityTaskSpec>();
            }
            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.Context;
        }

        public override void Execute()
        {
            var nodeData = GetNode();
            bool endAsCancelled = nodeData?.endType == EndAbilityType.Cancel;
            Spec.Context.AbilitySpec.As()?.EndAbility(endAsCancelled);
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return Spec.Context;
        }

        public override void OnInitialize()
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData != null)
            {
                selfSpec.EndAbilityType = nodeData.endType;
            }
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
