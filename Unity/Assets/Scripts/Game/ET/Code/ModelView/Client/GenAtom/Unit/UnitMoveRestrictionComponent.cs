namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class UnitMoveRestrictionComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<AbilitySystemComponent> ASC;
        public bool IsListening;
    }
}
