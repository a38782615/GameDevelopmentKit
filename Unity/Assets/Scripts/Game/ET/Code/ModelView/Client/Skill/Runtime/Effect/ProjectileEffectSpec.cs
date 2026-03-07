using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物效果Spec
    /// 负责生成投射物并管理其生命周期
    /// 注意：这是一个特殊的Effect，生命周期由投射物控制
    /// </summary>
    [ComponentOf(typeof(GameplayEffectSpec))]
    public class ProjectileEffectSpec : Entity, IAwake
    {
        public EntityRef<ProjectileRuntimeComponent> _projectileRuntime;
        public GameObject _projectileObject;
    }

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
