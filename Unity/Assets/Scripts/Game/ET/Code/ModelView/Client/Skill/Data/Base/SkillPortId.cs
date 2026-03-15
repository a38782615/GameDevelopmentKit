using System;

namespace ET.Client
{
    public static class SkillPortId
    {
        public const int Invalid = 0;

        public static class Ability
        {
            public const int Activate = 1001;
            public const int Animation = 1002;
            public const int Cost = 1003;
            public const int Cooldown = 1004;

            public const int EventOnHit = 1101;
            public const int EventOnDealDamage = 1102;
            public const int EventOnTakeDamage = 1103;
            public const int EventOnDeath = 1104;
            public const int EventOnKill = 1105;

            public const int EventDynamicBase = 1000000;
            public const int EventDynamicSpan = 900000;
        }

        public static class Condition
        {
            public const int True = 2001;
            public const int False = 2002;
        }

        public static class Effect
        {
            public const int Initial = 3001;
            public const int Periodic = 3002;
            public const int Refresh = 3003;
            public const int Complete = 3004;
            public const int RemoveAll = 3005;
            public const int Overflow = 3006;
        }

        public static class SearchTargetTask
        {
            public const int ForEachTarget = 4001;
            public const int Complete = 4002;
            public const int NoTarget = 4003;
        }

        public static class ProjectileEffect
        {
            public const int OnHit = 5001;
            public const int OnReachTarget = 5002;
            public const int OnBounce = 5003;
        }

        public static class PlacementEffect
        {
            public const int OnEnter = 6001;
            public const int OnExit = 6002;
        }

        public static class Input
        {
            public const int Default = 7001;

            public const int DynamicBase = 3000000;
            public const int DynamicSpan = 900000;
        }

        public static class Animation
        {
            public const int TrackDynamicBase = 2000000;
            public const int TrackDynamicSpan = 900000;
        }
    }

    public static class SkillPortIdUtility
    {
        public static int ResolveLegacyOutputPortId(NodeType nodeType, string legacyPortName)
        {
            if (string.IsNullOrEmpty(legacyPortName))
            {
                return SkillPortId.Invalid;
            }

            switch (nodeType)
            {
                case NodeType.Ability:
                    if (TryResolveAbilityFixedPortId(legacyPortName, out int abilityPortId))
                    {
                        return abilityPortId;
                    }

                    return ResolveAbilityEventPortId(legacyPortName);

                case NodeType.Animation:
                    return ResolveAnimationTrackPortId(legacyPortName);

                case NodeType.SearchTargetTask:
                    if (TryResolveSearchTargetTaskPortId(legacyPortName, out int taskPortId))
                    {
                        return taskPortId;
                    }

                    return SkillPortId.Invalid;

                case NodeType.ProjectileEffect:
                    if (TryResolveProjectileEffectPortId(legacyPortName, out int projectilePortId))
                    {
                        return projectilePortId;
                    }

                    return TryResolveEffectPortId(legacyPortName, out int projectileEffectPortId)
                        ? projectileEffectPortId
                        : SkillPortId.Invalid;

                case NodeType.PlacementEffect:
                    if (TryResolvePlacementEffectPortId(legacyPortName, out int placementPortId))
                    {
                        return placementPortId;
                    }

                    return TryResolveEffectPortId(legacyPortName, out int placementEffectPortId)
                        ? placementEffectPortId
                        : SkillPortId.Invalid;

                case NodeType.AttributeCompareCondition:
                    return TryResolveConditionPortId(legacyPortName, out int conditionPortId)
                        ? conditionPortId
                        : SkillPortId.Invalid;

                default:
                    return TryResolveEffectPortId(legacyPortName, out int effectPortId)
                        ? effectPortId
                        : SkillPortId.Invalid;
            }
        }

        public static int ResolveAbilityEventPortId(GameplayEventType eventType, string customEventTag)
        {
            switch (eventType)
            {
                case GameplayEventType.OnHit:
                    return SkillPortId.Ability.EventOnHit;
                case GameplayEventType.OnDealDamage:
                    return SkillPortId.Ability.EventOnDealDamage;
                case GameplayEventType.OnTakeDamage:
                    return SkillPortId.Ability.EventOnTakeDamage;
                case GameplayEventType.OnDeath:
                    return SkillPortId.Ability.EventOnDeath;
                case GameplayEventType.OnKill:
                    return SkillPortId.Ability.EventOnKill;
                case GameplayEventType.Custom:
                    return ResolveStableDynamicPortId(
                        SkillPortId.Ability.EventDynamicBase,
                        SkillPortId.Ability.EventDynamicSpan,
                        string.IsNullOrEmpty(customEventTag) ? "Custom" : customEventTag);
                default:
                    return ResolveStableDynamicPortId(
                        SkillPortId.Ability.EventDynamicBase,
                        SkillPortId.Ability.EventDynamicSpan,
                        eventType.ToString());
            }
        }

        public static int ResolveAbilityEventPortId(string legacyPortName)
        {
            if (TryResolveAbilityEventFixedPortId(legacyPortName, out int portId))
            {
                return portId;
            }

            return ResolveStableDynamicPortId(
                SkillPortId.Ability.EventDynamicBase,
                SkillPortId.Ability.EventDynamicSpan,
                legacyPortName);
        }

        public static int ResolveAnimationTrackPortId(string legacyPortId)
        {
            return ResolveStableDynamicPortId(
                SkillPortId.Animation.TrackDynamicBase,
                SkillPortId.Animation.TrackDynamicSpan,
                legacyPortId);
        }

        public static int ResolveLegacyInputPortId(string legacyPortName)
        {
            if (string.IsNullOrEmpty(legacyPortName))
            {
                return SkillPortId.Invalid;
            }

            if (legacyPortName == "\u8F93\u5165")
            {
                return SkillPortId.Input.Default;
            }

            return ResolveStableDynamicPortId(
                SkillPortId.Input.DynamicBase,
                SkillPortId.Input.DynamicSpan,
                legacyPortName);
        }

        public static int ResolveStableDynamicPortId(int dynamicBase, int dynamicSpan, string seed)
        {
            if (string.IsNullOrEmpty(seed))
            {
                return SkillPortId.Invalid;
            }

            int hash = GetPositiveHash(seed);
            return dynamicBase + hash % dynamicSpan;
        }

        private static bool TryResolveAbilityFixedPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "激活":
                    portId = SkillPortId.Ability.Activate;
                    return true;
                case "动画":
                    portId = SkillPortId.Ability.Animation;
                    return true;
                case "消耗":
                    portId = SkillPortId.Ability.Cost;
                    return true;
                case "冷却":
                    portId = SkillPortId.Ability.Cooldown;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolveAbilityEventFixedPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "受击时":
                    portId = SkillPortId.Ability.EventOnHit;
                    return true;
                case "造成伤害时":
                    portId = SkillPortId.Ability.EventOnDealDamage;
                    return true;
                case "受到伤害时":
                    portId = SkillPortId.Ability.EventOnTakeDamage;
                    return true;
                case "死亡时":
                    portId = SkillPortId.Ability.EventOnDeath;
                    return true;
                case "击杀时":
                    portId = SkillPortId.Ability.EventOnKill;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolveConditionPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "是":
                    portId = SkillPortId.Condition.True;
                    return true;
                case "否":
                    portId = SkillPortId.Condition.False;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolveEffectPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "初始效果":
                case "治疗":
                    portId = SkillPortId.Effect.Initial;
                    return true;
                case "每周期执行":
                    portId = SkillPortId.Effect.Periodic;
                    return true;
                case "刷新时":
                    portId = SkillPortId.Effect.Refresh;
                    return true;
                case "完成效果":
                    portId = SkillPortId.Effect.Complete;
                    return true;
                case "全部移除后":
                    portId = SkillPortId.Effect.RemoveAll;
                    return true;
                case "溢出":
                    portId = SkillPortId.Effect.Overflow;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolveSearchTargetTaskPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "对每个目标":
                    portId = SkillPortId.SearchTargetTask.ForEachTarget;
                    return true;
                case "完成效果":
                    portId = SkillPortId.SearchTargetTask.Complete;
                    return true;
                case "无目标":
                    portId = SkillPortId.SearchTargetTask.NoTarget;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolveProjectileEffectPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "碰撞时":
                    portId = SkillPortId.ProjectileEffect.OnHit;
                    return true;
                case "到达目标位置":
                    portId = SkillPortId.ProjectileEffect.OnReachTarget;
                    return true;
                case "反弹时":
                    portId = SkillPortId.ProjectileEffect.OnBounce;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static bool TryResolvePlacementEffectPortId(string legacyPortName, out int portId)
        {
            switch (legacyPortName)
            {
                case "进入时":
                    portId = SkillPortId.PlacementEffect.OnEnter;
                    return true;
                case "离开时":
                    portId = SkillPortId.PlacementEffect.OnExit;
                    return true;
                default:
                    portId = SkillPortId.Invalid;
                    return false;
            }
        }

        private static int GetPositiveHash(string value)
        {
            unchecked
            {
                int hash = 17;
                for (int index = 0; index < value.Length; ++index)
                {
                    hash = hash * 31 + value[index];
                }

                if (hash == int.MinValue)
                {
                    return 0;
                }

                return Math.Abs(hash);
            }
        }
    }
}
