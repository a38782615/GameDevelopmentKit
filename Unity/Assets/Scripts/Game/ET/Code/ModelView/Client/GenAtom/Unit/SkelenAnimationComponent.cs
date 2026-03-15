using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class SkelenAnimationComponent : Entity, IAwake, IDestroy
    {
        public EntityRef<AbilitySystemComponent> ASC;
        public bool IsListening;
        public bool IsStunned;
        public string StandAnimationName = "Stand";
        public string StunAnimationName = "Stun";
#if Spine
        public SkeletonAnimation SkeletonAnimation;
#endif
    }
}
