using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 投射物初始化数据
    /// </summary>
    public struct ProjectileInitData
    {
        public Vector2 LaunchPosition;
        public Vector2 TargetPosition;
        public Vector2 Direction;
        public AbilitySystemComponent TargetUnit;
        public ProjectileTargetType TargetType;
        public bool FlyOver;
        public float CurveHeight;
        public float Speed;
        public float MaxDistance;
        public float CollisionRadius;
        public bool IsPiercing;
        public int MaxPierceCount;
        public GameplayTagSet CollisionTargetTags;
        public GameplayTagSet CollisionExcludeTags;
        public string TargetBindingName;
        public string SkillId;
        public string NodeGuid;
        public SpecExecutionContext Context;
        public AbilitySystemComponent SourceASC;

        // 反弹设置
        public bool IsBouncing;
        public BounceTargetMode BounceTargetMode;
        public int MaxBounceCount;
        public float BounceSearchRadius;
        public bool CanBounceToSameTarget;
        public float BounceAngleOffset;
    }
}
