using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ProjectileEffectSpec : Entity, IAwake
    {
        public float2 ExpectedTargetPosition;
        public bool HasTriggeredHit;
        public bool IsLogicActive;
        public bool ReachedTarget;
        public float2 CurrentPosition;
        public float2 CurrentDirection;
        public float2 StartPosition;
        public float2 EndPosition;
        public float TraveledDistance;
        public float TotalDistance;
        public float FlightProgress;
        public int HitCount;
        public int BounceCount;
        public HashSet<long> HitTargetInstanceIds = new HashSet<long>();
    }
}
