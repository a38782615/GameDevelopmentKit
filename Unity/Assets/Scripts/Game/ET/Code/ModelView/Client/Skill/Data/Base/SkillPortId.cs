using System;
using Unity.Mathematics;

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
        public static int ResolveAbilityEventPortId(GameplayEventType eventType)
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
                default:
                    return SkillPortId.Ability.EventOnHit;
            }
        }

        public static int ResolveAnimationTrackPortId(string legacyPortId)
        {
            return ResolveStableDynamicPortId(
                SkillPortId.Animation.TrackDynamicBase,
                SkillPortId.Animation.TrackDynamicSpan,
                legacyPortId);
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

                return math.abs(hash);
            }
        }
    }
}
