

namespace ET.Client
{
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(TaskSpec))]
    [FriendOf(typeof(ConditionSpec))]
    [FriendOf(typeof(GameplayCueSpec))]
    public static class SpecFactory
    {
        public static void AttachEffectComponent(GameplayEffectSpec spec, NodeType nodeType)
        {
            if (spec == null) return;

            switch (nodeType)
            {
                case NodeType.DamageEffect:
                    spec.HandName = "DamageEffectSpecHandler";
                    EnsureEffectComponent<DamageEffectSpec>(spec);
                    return;
                case NodeType.HealEffect:
                    spec.HandName = "HealEffectSpecHandler";
                    EnsureEffectComponent<HealEffectSpec>(spec);
                    return;
                case NodeType.CostEffect:
                    spec.HandName = "CostEffectSpecHandler";
                    EnsureEffectComponent<CostEffectSpec>(spec);
                    return;
                case NodeType.ModifyAttributeEffect:
                    spec.HandName = "ModifyAttributeEffectSpecHandler";
                    EnsureEffectComponent<ModifyAttributeEffectSpec>(spec);
                    return;
                case NodeType.GenericEffect:
                    spec.HandName = "GenericEffectSpecHandler";
                    EnsureEffectComponent<GenericEffectSpec>(spec);
                    return;
                case NodeType.ProjectileEffect:
                    spec.HandName = "ProjectileEffectSpecHandler";
                    EnsureEffectComponent<ProjectileEffectSpec>(spec);
                    return;
                case NodeType.PlacementEffect:
                    spec.HandName = "PlacementEffectSpecHandler";
                    EnsureEffectComponent<PlacementEffectSpec>(spec);
                    return;
                case NodeType.DisplaceEffect:
                    spec.HandName = "DisplaceEffectSpecHandler";
                    EnsureEffectComponent<DisplaceEffectSpec>(spec);
                    return;
                case NodeType.CooldownEffect:
                    spec.HandName = "CooldownEffectSpecHandler";
                    EnsureEffectComponent<CooldownEffectSpec>(spec);
                    return;
                case NodeType.BuffEffect:
                    spec.HandName = "BuffEffectSpecHandler";
                    EnsureEffectComponent<BuffEffectSpec>(spec);
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        public static void AttachTaskComponent(TaskSpec spec, NodeType nodeType)
        {
            if (spec == null) return;

            switch (nodeType)
            {
                case NodeType.SearchTargetTask:
                    spec.HandName = "SearchTargetTaskSpecHandler";
                    EnsureTaskComponent<SearchTargetTaskSpec>(spec);
                    return;
                case NodeType.EndAbilityTask:
                    spec.HandName = "EndAbilityTaskSpecHandler";
                    EnsureTaskComponent<EndAbilityTaskSpec>(spec);
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        public static void AttachConditionComponent(ConditionSpec spec, NodeType nodeType)
        {
            if (spec == null) return;

            switch (nodeType)
            {
                case NodeType.AttributeCompareCondition:
                    spec.HandName = "AttributeCompareConditionHandler";
                    EnsureConditionComponent<AttributeCompareConditionSpec>(spec);
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        public static void AttachCueComponent(GameplayCueSpec spec, NodeType nodeType)
        {
            if (spec == null) return;

            switch (nodeType)
            {
                case NodeType.ParticleCue:
                    spec.HandName = "ParticleCueSpecHandler";
                    EnsureCueComponent<ParticleCueSpec>(spec);
                    return;
                case NodeType.SoundCue:
                    spec.HandName = "SoundCueSpecHandler";
                    EnsureCueComponent<SoundCueSpec>(spec);
                    return;
                case NodeType.FloatingTextCue:
                    spec.HandName = "FloatingTextCueSpecHandler";
                    EnsureCueComponent<FloatingTextCueSpec>(spec);
                    return;
                default:
                    spec.HandName = string.Empty;
                    return;
            }
        }

        private static void EnsureEffectComponent<T>(GameplayEffectSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        private static void EnsureTaskComponent<T>(TaskSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        private static void EnsureConditionComponent<T>(ConditionSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        private static void EnsureCueComponent<T>(GameplayCueSpec spec) where T : Entity, IAwake, new()
        {
            if (spec.GetComponent<T>() == null)
                spec.AddComponent<T>();
        }

        public static bool IsInstantEffect(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.DamageEffect:
                case NodeType.HealEffect:
                case NodeType.CostEffect:
                case NodeType.ModifyAttributeEffect:
                case NodeType.ProjectileEffect:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsDurationEffect(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.CooldownEffect:
                case NodeType.BuffEffect:
                case NodeType.PlacementEffect:
                case NodeType.DisplaceEffect:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTaskNode(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.SearchTargetTask:
                case NodeType.EndAbilityTask:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsConditionNode(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.AttributeCompareCondition:
                    return true;
                default:
                    return false;
            }
        }
    }
}
