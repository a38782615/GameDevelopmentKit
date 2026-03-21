namespace ET
{
    [ComponentOf(typeof(Unit))]
    public class MoveRestrictionComponent : Entity, IAwake
    {
        public bool IsBlocked;
    }
}
