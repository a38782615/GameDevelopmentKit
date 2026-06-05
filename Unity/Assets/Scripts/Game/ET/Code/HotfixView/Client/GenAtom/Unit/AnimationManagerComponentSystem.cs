using System;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    [EntitySystemOf(typeof(AnimationManagerComponent))]
    [FriendOf(typeof(AnimationManagerComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(SkillUnit))]
    public static partial class AnimationManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AnimationManagerComponent self)
        {
            self.Bind();
        }

        [EntitySystem]
        private static void Destroy(this AnimationManagerComponent self)
        {
            self.UnregisterTagListeners();
            self.ASC = default;
        }

        public static void Bind(this AnimationManagerComponent self)
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

            self.ResolveDriverType();

            switch (self.ResolvedDriverType)
            {
                case AnimationDriverType.Skelen:
                    unit.GetOrAddComponent<SkelenAnimationComponent>().Bind();
                    break;
                case AnimationDriverType.Unity:
                    unit.GetOrAddComponent<UnityAnimationComponent>().Bind(string.Empty);
                    break;
            }

            self.RegisterTagListeners();
        }

        public static void SetDriverType(this AnimationManagerComponent self, AnimationDriverType driverType)
        {
            self.DriverType = driverType;
            self.ResolvedDriverType = AnimationDriverType.Auto;
            self.Bind();
        }

        public static void PlayAnimation(this AnimationManagerComponent self, string name, bool loop)
        {
            self.PlayAnimation(name, loop, string.Empty);
        }

        public static void PlayAnimation(this AnimationManagerComponent self, string name, bool loop, string animationComponentPath)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            self.Bind();
            Unit unit = self.GetParent<Unit>();
            if (unit == null)
            {
                return;
            }

            switch (self.ResolvedDriverType)
            {
                case AnimationDriverType.Skelen:
                    unit.GetOrAddComponent<SkelenAnimationComponent>().PlayAnimation(name, loop);
                    break;
                case AnimationDriverType.Unity:
                    unit.GetOrAddComponent<UnityAnimationComponent>().PlayAnimation(name, loop, animationComponentPath);
                    break;
            }
        }

        public static bool IsAnimationStunned(this AnimationManagerComponent self)
        {
            self.Bind();
            return self.IsStunned;
        }

        public static void PlayMoveAnimation(this AnimationManagerComponent self)
        {
            if (self.IsAnimationStunned())
            {
                return;
            }

            self.PlayAnimation(self.MoveAnimationName, true);
        }

        public static void PlayStandAnimation(this AnimationManagerComponent self)
        {
            if (self.IsAnimationStunned())
            {
                return;
            }

            self.PlayAnimation(self.StandAnimationName, true);
        }

        private static void ResolveDriverType(this AnimationManagerComponent self)
        {
            if (self.DriverType != AnimationDriverType.Auto)
            {
                self.ResolvedDriverType = self.DriverType;
                return;
            }

            Unit unit = self.GetParent<Unit>();
            UnityEngine.GameObject viewGameObject = unit?.GetComponent<GameObjectComponent>()?.GameObject;
            if (viewGameObject == null)
            {
                self.ResolvedDriverType = AnimationDriverType.Auto;
                return;
            }

#if Spine
            if (viewGameObject.GetComponentInChildren<SkeletonAnimation>(true) != null)
            {
                self.ResolvedDriverType = AnimationDriverType.Skelen;
                return;
            }
#endif

            if (viewGameObject.GetComponentInChildren<UnityEngine.Animation>(true) != null)
            {
                self.ResolvedDriverType = AnimationDriverType.Unity;
                return;
            }

            self.ResolvedDriverType = AnimationDriverType.Auto;
        }

        private static void RegisterTagListeners(this AnimationManagerComponent self)
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

        private static void UnregisterTagListeners(this AnimationManagerComponent self)
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

        private static void OnTagAdded(this AnimationManagerComponent self, GameplayTag tag)
        {
            if (self.IsStunned || tag != GameplayTagLibrary.Buff_DeBuff_Stun)
            {
                return;
            }

            self.IsStunned = true;
            self.PlayAnimation(self.StunAnimationName, true);
        }

        private static void OnTagRemoved(this AnimationManagerComponent self, GameplayTag tag)
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
