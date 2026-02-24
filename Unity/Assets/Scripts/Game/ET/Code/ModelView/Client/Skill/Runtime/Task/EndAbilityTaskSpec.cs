namespace ET.Client
{
    [ComponentOf(typeof(TaskSpec))]
    public partial class EndAbilityTaskSpec : Entity, IAwake
    {
        public EndAbilityType EndAbilityType;
    }
}