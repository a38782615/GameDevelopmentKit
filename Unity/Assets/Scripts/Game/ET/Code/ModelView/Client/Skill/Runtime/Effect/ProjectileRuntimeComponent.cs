using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(ProjectileEffectSpec))]
    public class ProjectileRuntimeComponent : Entity, IAwake<ProjectileInitData>, IDestroy
    {
        public ProjectileInitData Data;
        public bool IsInitialized;
        public bool ReachedTarget;

        public Vector2 CurrentPosition;
        public Vector2 CurrentDirection;
        public float TraveledDistance;
        public float TotalDistance;
        public int HitCount;
        public int BounceCount;

        public Vector2 StartPosition;
        public Vector2 EndPosition;
        public float FlightProgress;

        public HashSet<long> HitTargetIds = new();
    }
}