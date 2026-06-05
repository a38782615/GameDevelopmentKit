using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class UnityAnimationComponent : Entity, IAwake, IDestroy
    {
        public Animation Animation;
        public string AnimationComponentPath = string.Empty;
    }
}
