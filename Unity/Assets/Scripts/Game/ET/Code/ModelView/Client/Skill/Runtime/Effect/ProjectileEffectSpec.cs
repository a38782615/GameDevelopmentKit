using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ProjectileEffectSpec : Entity, IAwake
    {
        public EntityRef<UGFEntityProjectile> ProjectileEntity;
        public Vector2 ExpectedTargetPosition;
        public bool HasTriggeredHit;
        public bool IsLogicActive;
        public bool ReachedTarget;
        public Vector2 CurrentPosition;
        public Vector2 CurrentDirection;
        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public float TraveledDistance;
        public float TotalDistance;
        public float FlightProgress;
        public int HitCount;
        public int BounceCount;
        public HashSet<long> HitTargetInstanceIds = new HashSet<long>();
    }
}
