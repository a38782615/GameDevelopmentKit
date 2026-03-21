using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(ActiveCueComponent))]
    [FriendOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class GameplayCueSpecSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayCueSpec self)
        {
        }

        [EntitySystem]
        private static void Update(this GameplayCueSpec self)
        {
            ActiveCueComponent activeCue = self.GetActiveCue();
            if (!self.IsRunning || activeCue == null)
            {
                return;
            }

            activeCue.Tick(UnityEngine.Time.deltaTime);
            if (!activeCue.IsExpired)
            {
                return;
            }

            self.IsRunning = false;
            self.RemoveActiveCueComponent();
        }

        [EntitySystem]
        private static void Destroy(this GameplayCueSpec self)
        {
            if (self.IsRunning)
            {
                self.GetCue().StopCue();
            }

            self.RemoveActiveCueComponent();
            self.Context = default;
            self.ContextOwner = default;
        }

        public static bool CanPlayOnTarget(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            if (target == null)
            {
                return true;
            }

            if (!self.Tags.RequiredTags.IsEmpty && !target.OwnedTags.HasAllTags(self.Tags.RequiredTags))
            {
                return false;
            }

            if (!self.Tags.ImmunityTags.IsEmpty && target.OwnedTags.HasAnyTags(self.Tags.ImmunityTags))
            {
                return false;
            }

            return true;
        }

        public static AbilitySystemComponent GetCueTarget(this GameplayCueSpec self)
        {
            SpecExecutionContext context = self.GetContext();
            NodeData nodeData = self.NodeData;
            if (nodeData == null)
            {
                return context?.GetMainTarget();
            }

            return context?.GetTargetByType(nodeData.targetType);
        }

        public static List<AbilitySystemComponent> GetCueTargets(this GameplayCueSpec self)
        {
            SpecExecutionContext context = self.GetContext();
            NodeData nodeData = self.NodeData;
            if (context == null)
            {
                return null;
            }

            if (nodeData == null)
            {
                List<AbilitySystemComponent> result = new List<AbilitySystemComponent>();
                foreach (EntityRef<AbilitySystemComponent> ignored in context.Targets)
                {
                    AbilitySystemComponent asc = context.GetTargetByType(TargetType.MainTarget);
                    if (asc != null)
                    {
                        result.Add(asc);
                    }
                }

                return result;
            }

            return context.GetTargetsByType(nodeData.targetType);
        }

        public static UnityEngine.Transform GetTargetTransform(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            return target?.Owner?.transform;
        }

        public static UnityEngine.Vector3 GetTargetPosition(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            UnityEngine.Transform transform = self.GetTargetTransform(target);
            return transform != null ? transform.position : UnityEngine.Vector3.zero;
        }

        public static SpecExecutionContext GetContext(this GameplayCueSpec self)
        {
            SpecExecutionContext context = self.Context;
            if (context != null)
            {
                return context;
            }

            GameplayAbilitySpec gameplayAbilitySpec = self.ContextOwner.As();
            if (gameplayAbilitySpec == null)
            {
                return null;
            }

            return gameplayAbilitySpec.Context.As();
        }

        public static ActiveCueComponent GetActiveCue(this GameplayCueSpec self)
        {
            return self?.ActiveCueComponent.As();
        }

        public static ActiveCueComponent EnsureActiveCueComponent(this GameplayCueSpec self, bool isLooping)
        {
            if (self == null)
            {
                return null;
            }

            ActiveCueComponent activeCue = self.ActiveCueComponent;
            if (activeCue == null)
            {
                activeCue = self.AddComponent<ActiveCueComponent>();
                self.ActiveCueComponent = activeCue;
            }

            activeCue.ResetForPlay(isLooping);
            return activeCue;
        }

        public static void RemoveActiveCueComponent(this GameplayCueSpec self)
        {
            if (self == null)
            {
                return;
            }

            ActiveCueComponent activeCue = self.ActiveCueComponent;
            if (activeCue != null)
            {
                activeCue.Stop();
                if (!activeCue.IsDisposed)
                {
                    self.RemoveComponent<ActiveCueComponent>();
                }
            }

            self.ActiveCueComponent = default;
        }

        public static ACueHandler GetCue(this GameplayCueSpec self)
        {
            ACueHandler cue = CueDispatcherComponent.Instance.Get(self.HandName);
            cue.Spec = self;
            return cue;
        }
    }
}
