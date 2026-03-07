using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(GameplayCueSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
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

        // ============ 初始化 ============

        public static void InitCue(this GameplayCueSpec self, string skillId, string nodeGuid, GameplayAbilitySpec contextOwner, string cueName)
        {
            self.SkillId = skillId;
            self.NodeGuid = nodeGuid;
            self.ContextOwner = contextOwner;
            self.IsRunning = false;
            self.IsCancelled = false;
            self.HandName = cueName;

            var cueData = self.CueNodeData;
            if (cueData != null)
                self.Tags = new CueTagContainer(cueData);

            self.GetCue().OnInitialize();
        }

        // ============ 执行 ============

        public static void ExecuteCue(this GameplayCueSpec self)
        {
            var context = self.GetContext();
            if (context == null) return;

            var target = self.GetCueTarget();

            if (!self.CanPlayOnTarget(target)) return;

            self.GetCue().PlayCue(target);
        }

        public static void TickCue(this GameplayCueSpec self, float deltaTime)
        {
            if (!self.IsRunning || self.ActiveCue == null) return;

            if (self.ActiveCue.IsExpired)
            {
                self.IsRunning = false;
                self.ActiveCue = null;
            }
        }

        public static void CancelCue(this GameplayCueSpec self)
        {
            self.IsCancelled = true;
            self.IsRunning = false;
            self.GetCue().StopCue();
        }

        public static void StopCuePublic(this GameplayCueSpec self)
        {
            if (!self.IsRunning) return;
            self.IsRunning = false;
            self.GetCue().StopCue();
        }

        public static void ResetCue(this GameplayCueSpec self)
        {
            self.IsRunning = false;
            self.IsCancelled = false;
            self.ActiveCue = null;
        }

        // ============ 检查方法 ============

        public static bool CanPlayOnTarget(this GameplayCueSpec self, AbilitySystemComponent target)
        {
            if (target == null) return true; // 无目标时允许播放（世界空间Cue）

            if (!self.Tags.RequiredTags.IsEmpty)
            {
                if (!target.HasAllTags(self.Tags.RequiredTags))
                    return false;
            }

            if (!self.Tags.ImmunityTags.IsEmpty)
            {
                if (target.HasAnyTags(self.Tags.ImmunityTags))
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

        public static List<AbilitySystemComponent> GetCueTargets(this GameplayCueSpec self, SpecExecutionContext context)
        {
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
            var gameplayAbilitySpec = self.ContextOwner.As();
            if (gameplayAbilitySpec == null) return null;
            var ret = gameplayAbilitySpec.Context;
            return ret;
        }


        public static ACueHandler GetCue(this GameplayCueSpec self)
        {
            var cue = CueDispatcherComponent.Instance.Get(self.HandName);
            cue.Spec = self;
            return cue;
        }
    }
}
