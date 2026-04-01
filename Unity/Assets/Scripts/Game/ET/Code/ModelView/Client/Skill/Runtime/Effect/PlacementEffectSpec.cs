using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class PlacementEffectSpec : Entity, IAwake
    {
        public bool IsLogicActive;
        public float3 RuntimePosition;
        public Dictionary<long, EntityRef<AbilitySystemComponent>> CurrentTargets = new Dictionary<long, EntityRef<AbilitySystemComponent>>();
    }
}
