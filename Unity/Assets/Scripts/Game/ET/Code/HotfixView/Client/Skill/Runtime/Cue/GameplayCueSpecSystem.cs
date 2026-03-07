using System.Collections.Generic;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
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

            var ownedTags = target.OwnedTags;
            if (ownedTags == null)
            {
                return self.Tags.RequiredTags.IsEmpty;
            }

            if (!self.Tags.RequiredTags.IsEmpty)
            {
                if (!ownedTags.HasAllTags(self.Tags.RequiredTags))
                    return false;
            }

            if (!self.Tags.ImmunityTags.IsEmpty)
            {
                if (ownedTags.HasAnyTags(self.Tags.ImmunityTags))
                    return false;
            }

            return true;
        }

        // ============ 辅助方法 ============

        public static AbilitySystemComponent GetCueTarget(this GameplayCueSpec self)
        {
            var context = self.GetContext();
            var nodeData = self.NodeData;
            if (context == null)
            {
                return null;
            }

            return GetTargetByType(context, nodeData == null ? TargetType.MainTarget : nodeData.targetType);
        }

        public static List<AbilitySystemComponent> GetCueTargets(this GameplayCueSpec self, SpecExecutionContext context)
        {
            if (context == null)
            {
                return null;
            }

            var nodeData = self.NodeData;
            if (nodeData == null)
            {
                var result = new List<AbilitySystemComponent>();
                foreach (var id in context.Targets)
                {
                    var asc = id.As();
                    if (asc != null)
                    {
                        result.Add(asc);
                    }
                }
                return result;
            }

            return GetTargetsByType(context, nodeData.targetType);
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

        private static AbilitySystemComponent GetTargetByType(SpecExecutionContext context, TargetType targetType)
        {
            switch (targetType)
            {
                case TargetType.Caster:
                    return context.Caster;
                case TargetType.MainTarget:
                    return context.MainTarget;
                case TargetType.ParentInput:
                    return context.ParentInputTarget;
                default:
                    return context.MainTarget;
            }
        }

        private static List<AbilitySystemComponent> GetTargetsByType(SpecExecutionContext context, TargetType targetType)
        {
            var result = new List<AbilitySystemComponent>();
            switch (targetType)
            {
                case TargetType.Caster:
                    if (context.Caster.As() != null)
                    {
                        result.Add(context.Caster);
                    }
                    break;
                case TargetType.MainTarget:
                    if (context.MainTarget.As() != null)
                    {
                        result.Add(context.MainTarget);
                    }
                    break;
                case TargetType.ParentInput:
                    if (context.ParentInputTarget.As() != null)
                    {
                        result.Add(context.ParentInputTarget);
                    }
                    break;
                default:
                    foreach (var target in context.Targets)
                    {
                        var asc = target.As();
                        if (asc != null)
                        {
                            result.Add(asc);
                        }
                    }
                    break;
            }

            return result;
        }
    }
}
