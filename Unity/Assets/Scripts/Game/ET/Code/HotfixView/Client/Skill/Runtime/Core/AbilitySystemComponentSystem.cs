namespace ET.Client
{
    [EntitySystemOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayEffectContainerComponent))]
    [FriendOf(typeof(GameplayEffectSpec))]
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
            self.Attributes.OnAnyAttributeChanged += self.OnAnyAttributeChanged;
        }

        public static void OnAnyAttributeChanged(this AbilitySystemComponent self, Attribute attribute, float before, float after)
        {
            if (attribute.AttrType == AttrType.Health)
            {
                if (after < before)
                {
                    EventSystem.Instance.Invoke(new AbilitySystemComponent.OnTGameplayEvent()
                    {
                        GameplayEventType = GameplayEventType.OnTakeDamage
                    });
                }
            }
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
                EventSystem.Instance.Invoke(new AbilitySystemComponent.OnAbilityActivated()
                {
                    Spec = spec
                });
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
            EventSystem.Instance.Invoke(new AbilitySystemComponent.OnAbilityEnded()
            {
                Spec = spec,
                End = wasCancelled
            });
        }

        // ============ 效果相关 ============

        public static bool RemoveActiveEffect(this AbilitySystemComponent self, GameplayEffectSpec effectSpec)
        {
            var container = self.EffectContainer;
            if (container == null || effectSpec == null || !container.ActiveEffects.Contains(effectSpec))
            {
                return false;
            }

            if (container.IsUpdating)
            {
                if (!container.PendingRemove.Contains(effectSpec))
                {
                    container.PendingRemove.Add(effectSpec);
                }
            }
            else
            {
                effectSpec.RemoveEffect();
                container.ActiveEffects.Remove(effectSpec);
                if (!effectSpec.IsDisposed)
                {
                    effectSpec.Dispose();
                }
            }

            return true;
        }

        public static int RemoveActiveEffectsWithTags(this AbilitySystemComponent self, GameplayTagSet tags)
        {
            var container = self.EffectContainer;
            if (container == null || tags.IsEmpty)
            {
                return 0;
            }

            int removedCount = 0;
            for (int i = container.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var effect = container.ActiveEffects[i].As();
                if (effect != null && effect.Tags.AssetTags.HasAnyTags(tags))
                {
                    if (self.RemoveActiveEffect(effect))
                    {
                        removedCount++;
                    }
                }
            }

            return removedCount;
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
