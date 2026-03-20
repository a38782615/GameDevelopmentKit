using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayAbilitySpec))]
    public static partial class GameplayCueSpecSystem
    {
        [EntitySystem]
        private static void Awake(this GameplayCueSpec self)
        {
        }

        [EntitySystem]
        private static void Update(this GameplayCueSpec self)
        {
            if (!self.IsRunning || self.ActiveCue == null) return;

            if (self.ActiveCue.IsExpired)
            {
                self.IsRunning = false;
                self.ActiveCue = null;
            }
        }

        [EntitySystem]
        private static void Destroy(this GameplayCueSpec self)
        {
            if (self.IsRunning)
                self.GetCue().StopCue();
            self.ActiveCue = null;
        }

        public static bool CanPlayOnTarget(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            if (target == null) return true; // 无目标时允许播放（世界空间Cue）

            if (!self.Tags.RequiredTags.IsEmpty)
            {
                if (!target.OwnedTags.HasAllTags(self.Tags.RequiredTags))
                    return false;
            }

            if (!self.Tags.ImmunityTags.IsEmpty)
            {
                if (target.OwnedTags.HasAnyTags(self.Tags.ImmunityTags))
                    return false;
            }

            return true;
        }

        // ============ 辅助方法 ============

        public static AbilitySystemComponent GetCueTarget(this GameplayCueSpec self)
        {
            var context = self.GetContext();
            var nodeData = self.NodeData;
            if (nodeData == null) return context?.GetMainTarget();
            return context?.GetTargetByType(nodeData.targetType);
        }

        public static List<AbilitySystemComponent> GetCueTargets(this GameplayCueSpec self)
        {
            var context = self.GetContext();
            var nodeData = self.NodeData;
            if (nodeData == null)
            {
                var result = new List<AbilitySystemComponent>();
                foreach (var id in context.Targets)
                {
                    var asc = context.GetTargetByType(TargetType.MainTarget);
                    if (asc != null) result.Add(asc);
                }
                return result;
            }
            return context?.GetTargetsByType(nodeData.targetType);
        }

        public static UnityEngine.Transform GetTargetTransform(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            return target?.Owner?.transform;
        }

        public static UnityEngine.Vector3 GetTargetPosition(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            var transform = self.GetTargetTransform(target);
            return transform != null ? transform.position : UnityEngine.Vector3.zero;
        }


        /// <summary>
        /// 获取执行上下文
        /// </summary>
        public static SpecExecutionContext GetContext(this GameplayCueSpec self)
        {
            SpecExecutionContext context = self.Context;
            if (context != null)
            {
                return context;
            }

            var gameplayAbilitySpec = self.ContextOwner.As();
            if (gameplayAbilitySpec == null) return null;
            return gameplayAbilitySpec.Context.As();
        }


        public static ACueHandler GetCue(this GameplayCueSpec self)
        {
            var cue = CueDispatcherComponent.Instance.Get(self.HandName);
            cue.Spec = self;
            return cue;
        }
    }
}
