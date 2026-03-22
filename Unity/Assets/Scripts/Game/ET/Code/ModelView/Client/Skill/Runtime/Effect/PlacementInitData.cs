using UnityEngine;

namespace ET.Client
{
    public struct PlacementInitData
    {
        public Vector3 Position;
        public bool EnableCollision;
        public float CollisionRadius;
        public GameplayTagSet CollisionTargetTags;
        public GameplayTagSet CollisionExcludeTags;
        public EntityRef<AbilitySystemComponent> SourceASC;
    }
}
