using System;
using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [EntitySystemOf(typeof(SkelenAnimationComponent))]
    [FriendOf(typeof(SkelenAnimationComponent))]
    public static partial class SkelenAnimationComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SkelenAnimationComponent self)
        {
            self.Bind();
        }

        [EntitySystem]
        private static void Destroy(this SkelenAnimationComponent self)
        {
#if Spine
            self.SkeletonAnimation = null;
#endif
        }

        public static void Bind(this SkelenAnimationComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit == null)
            {
                return;
            }

#if Spine
            if (self.SkeletonAnimation == null)
            {
                GameObject viewGameObject = unit.GetComponent<GameObjectComponent>()?.GameObject;
                if (viewGameObject != null)
                {
                    self.SkeletonAnimation = viewGameObject.GetComponentInChildren<SkeletonAnimation>(true);
                }
            }
#endif
        }

        public static void PlayAnimation(this SkelenAnimationComponent self, string name, bool loop)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            self.Bind();
#if Spine
            if (self.SkeletonAnimation?.AnimationState == null)
            {
                return;
            }

            var current = self.SkeletonAnimation.AnimationState.GetCurrent(0);
            if (current?.Animation?.Name == name)
            {
                return;
            }

            self.SkeletonAnimation.AnimationState.SetAnimation(0, name, loop);
#endif
        }
    }
}
