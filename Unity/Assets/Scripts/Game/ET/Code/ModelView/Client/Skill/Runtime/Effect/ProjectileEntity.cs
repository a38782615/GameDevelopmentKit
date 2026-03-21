using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    [ChildOf(typeof(GameplayEffectSpec))]
    public class ProjectileEntity : UGFEntity, IAwake<ProjectileInitData>, IUGFEntityOnShow, IUGFEntityOnUpdate, IUGFEntityOnHide
    {
        public ProjectileInitData InitData;
        public bool Initialized;
        public bool DestroyRequested;
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
