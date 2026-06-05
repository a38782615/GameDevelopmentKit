using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class SkelenAnimationComponent : Entity, IAwake, IDestroy
    {
#if Spine
        public SkeletonAnimation SkeletonAnimation;
#endif
    }
}
