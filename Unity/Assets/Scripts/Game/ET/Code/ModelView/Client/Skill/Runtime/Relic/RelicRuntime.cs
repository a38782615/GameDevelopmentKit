namespace ET.Client
{
    [ChildOf(typeof(RelicContainerComponent))]
    public partial class RelicRuntime : Entity, IAwake<long>
    {
        public int RelicId;
        public int EffectType;
        public float EffectValue;
        public int TriggerType;
    }
}
