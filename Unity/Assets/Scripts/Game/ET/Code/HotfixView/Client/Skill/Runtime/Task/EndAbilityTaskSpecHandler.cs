namespace ET.Client
{
    [TaskHandler]
    public class EndAbilityTaskSpecHandler : ATaskHandler
    {
        public override SpecExecutionContext GetContext()
        {
            return this.Spec?.GetContext();
        }

        public override void Execute()
        {
            var nodeData = this.NodeData as EndAbilityTaskNodeData;
            GameplayAbilitySpec abilitySpec = this.GetContext()?.GetAbilitySpec();
            if (nodeData == null || abilitySpec == null)
                return;

            if (nodeData.endType == EndAbilityType.Cancel)
                abilitySpec.CancelAbility();
            else
                abilitySpec.EndAbility();
        }
    }
}
