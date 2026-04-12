namespace ET.Client
{
    [ChildOf(typeof(SkillCardDeckComponent))]
    public partial class SkillCardRuntime : Entity, IAwake<long>
    {
        public long CardInstanceId;
        public int SkillId;
        public EntityRef<GameplayAbilitySpec> SpecRef;
        public SkillCardZone Zone;
        public float BaseCostMp;
        public float OverrideCostMp;
        public bool HasOverrideCostMp;
        public int TriggerType;
    }
}
