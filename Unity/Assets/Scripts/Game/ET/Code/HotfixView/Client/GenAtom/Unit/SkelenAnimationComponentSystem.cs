using System;
using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [EntitySystemOf(typeof(SkelenAnimationComponent))]
    [FriendOf(typeof(SkelenAnimationComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(SkillUnit))]
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
            self.UnregisterTagListeners();
            self.ASC = default;
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

            SkillUnit skillUnit = unit.GetComponent<SkillUnit>();
            AbilitySystemComponent asc = skillUnit?.ASC.As();
            if (!ReferenceEquals(self.ASC.As(), asc))
            {
                self.UnregisterTagListeners();
                self.ASC = asc;
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

            self.RegisterTagListeners();
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

        private static void RegisterTagListeners(this SkelenAnimationComponent self)
        {
            if (self.IsListening)
            {
                return;
            }

            AbilitySystemComponent asc = self.ASC.As();
            if (asc?.OwnedTags == null)
            {
                return;
            }

            asc.OwnedTags.OnTagAdded += self.OnTagAdded;
            asc.OwnedTags.OnTagRemoved += self.OnTagRemoved;
            self.IsListening = true;
            self.IsStunned = asc.OwnedTags.HasTag(GameplayTagLibrary.Buff_DeBuff_Stun);
            if (self.IsStunned)
            {
                self.PlayAnimation(self.StunAnimationName, true);
            }
        }

        private static void UnregisterTagListeners(this SkelenAnimationComponent self)
        {
            if (!self.IsListening)
            {
                return;
            }

            AbilitySystemComponent asc = self.ASC.As();
            if (asc?.OwnedTags != null)
            {
                asc.OwnedTags.OnTagAdded -= self.OnTagAdded;
                asc.OwnedTags.OnTagRemoved -= self.OnTagRemoved;
            }

            self.IsListening = false;
        }

        private static void OnTagAdded(this SkelenAnimationComponent self, GameplayTag tag)
        {
            if (self.IsStunned || tag != GameplayTagLibrary.Buff_DeBuff_Stun)
            {
                return;
            }

            self.IsStunned = true;
            self.PlayAnimation(self.StunAnimationName, true);
        }

        private static void OnTagRemoved(this SkelenAnimationComponent self, GameplayTag tag)
        {
            AbilitySystemComponent asc = self.ASC.As();
            if (!self.IsStunned || tag != GameplayTagLibrary.Buff_DeBuff_Stun || asc == null || asc.OwnedTags.HasTag(GameplayTagLibrary.Buff_DeBuff_Stun))
            {
                return;
            }

            self.IsStunned = false;
            self.PlayAnimation(self.StandAnimationName, true);
        }
    }
}
