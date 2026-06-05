namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class AnimationManagerComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<AbilitySystemComponent> ASC;
        public bool IsListening;
        public bool IsStunned;
        public string StandAnimationName = "Stand";
        public string MoveAnimationName = "Move";
        public string StunAnimationName = "Stun";
        public AnimationDriverType DriverType = AnimationDriverType.Auto;
        public AnimationDriverType ResolvedDriverType;
    }
}
