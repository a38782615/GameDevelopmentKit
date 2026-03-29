namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class PlacementEffectSpec : Entity, IAwake
    {
        public EntityRef<UGFEntityPlacement> PlacementEntity;
    }
}
