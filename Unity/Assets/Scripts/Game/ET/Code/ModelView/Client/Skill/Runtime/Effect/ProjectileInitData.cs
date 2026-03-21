using UnityEngine;

namespace ET.Client
{
    public struct ProjectileInitData
    {
        public Vector2 LaunchPosition;
        public Vector2 TargetPosition;
        public Vector2 Direction;
        public EntityRef<AbilitySystemComponent> TargetUnit;
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
        public EntityRef<AbilitySystemComponent> SourceASC;
        public bool IsBouncing;
        public BounceTargetMode BounceTargetMode;
        public int MaxBounceCount;
        public float BounceSearchRadius;
        public bool CanBounceToSameTarget;
        public bool ExcludeSourceCamp;
        public float BounceAngleOffset;
    }
}
