namespace ET.Client
{
    [EntitySystemOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static partial class AbilitySystemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AbilitySystemComponent self)
        {
            self.Attributes = new AttributeSetContainer();
            self.OwnedTags = new GameplayTagContainer();
            self.IsInitialized = true;

            // 添加子组件
            self.AddComponent<AbilityContainerComponent>();
            self.AddComponent<GameplayEffectContainerComponent>();
            self.AddComponent<GameplayCueContainerComponent>();

            // 订阅属性变化事件
            self.Attributes.OnAnyAttributeChanged += (attribute, before, after) =>
            {
                if (attribute.AttrType == AttrType.Health)
                {
                    if (after < before)
                    {
                        // self.FireGameplayEvent(GameplayEventType.OnTakeDamage);
                    }
                }
            };
        }

        [EntitySystem]
        private static void Update(this AbilitySystemComponent self)
        {
            // IUpdate 自动驱动，不再需要 GASHost
        }

        [EntitySystem]
        private static void Destroy(this AbilitySystemComponent self)
        {
            self.OwnedTags?.Clear();
            self.Attributes?.Clear();
            self.IsInitialized = false;
        }

        // ============ 技能相关 ============

        public static GameplayAbilitySpec GrantAbility(this AbilitySystemComponent self, SkillData abilityData)
        {
            if (abilityData == null) return null;
            return self.Abilities?.GrantAbility(self, abilityData);
        }

        public static bool RemoveAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec)
        {
            return self.Abilities?.RemoveAbility(spec) ?? false;
        }

        public static bool TryActivateAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec, AbilitySystemComponent target = null)
        {
            if (spec == null) return false;

            bool success = self.Abilities?.TryActivateAbility(self, spec, target) ?? false;
            if (success)
            {
                // self.FireAbilityActivated(spec);
                // spec.OnEnded += (s, wasCancelled) => self.FireAbilityEnded(s, wasCancelled);
            }
            return success;
        }

        public static void CancelAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec)
        {
            self.Abilities?.CancelAbility(spec);
        }

        public static void EndAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec, bool wasCancelled = false)
        {
            self.Abilities?.EndAbility(spec, wasCancelled);
        }

        // ============ 效果相关 ============

        public static bool RemoveActiveEffect(this AbilitySystemComponent self, GameplayEffectSpec effectSpec)
        {
            return self.EffectContainer?.RemoveEffect(effectSpec) ?? false;
        }

        public static int RemoveActiveEffectsWithTags(this AbilitySystemComponent self, GameplayTagSet tags)
        {
            return self.EffectContainer?.RemoveEffectsWithTags(tags) ?? 0;
        }

        // ============ 标签相关 ============

        public static bool HasTag(this AbilitySystemComponent self, GameplayTag tag)
        {
            return self.OwnedTags.HasTag(tag);
        }

        public static bool HasAllTags(this AbilitySystemComponent self, GameplayTagSet tags)
        {
            return self.OwnedTags.HasAllTags(tags);
        }

        public static bool HasAnyTags(this AbilitySystemComponent self, GameplayTagSet tags)
        {
            return self.OwnedTags.HasAnyTags(tags);
        }

        public static bool HasNoneTags(this AbilitySystemComponent self, GameplayTagSet tags)
        {
            return self.OwnedTags.HasNoneTags(tags);
        }
    }
}
