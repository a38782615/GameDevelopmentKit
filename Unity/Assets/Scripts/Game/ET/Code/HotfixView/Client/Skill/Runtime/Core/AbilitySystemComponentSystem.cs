namespace ET.Client
{
    [EntitySystemOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(AbilityContainerComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(GameplayEffectContainerComponent))]
    [FriendOf(typeof(GameplayEffectSpec))]
    public static partial class AbilitySystemComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AbilitySystemComponent self)
        {
            self.OwnedTags = new GameplayTagContainer();
            self.IsInitialized = true;

            SkillUnit skillUnit = self.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            if (unit != null && unit.GetComponent<global::ET.AttributeComponent>() == null)
            {
                unit.AddComponent<global::ET.AttributeComponent>();
            }

            // 添加子组件
            self.AddComponent<AbilityContainerComponent>();
            self.AddComponent<GameplayEffectContainerComponent>();
            self.AddComponent<GameplayCueContainerComponent>();

            // 订阅属性变化事件
            AttributeSetContainer attributes = self.Attributes;
            if (attributes != null)
            {
                attributes.OnAnyAttributeChanged += self.OnAnyAttributeChanged;
            }
        }

        public static void OnAnyAttributeChanged(this AbilitySystemComponent self, Attribute attribute, float before, float after)
        {
            if (self.Owner != null && (attribute.AttrType == AttrType.Health || attribute.AttrType == AttrType.MaxHealth))
            {
                SkillHudManager.GetOrCreate().UpdateUnitHealth(
                    self.InstanceId,
                    self.Owner,
                    self.Attributes?.GetCurrentValue(AttrType.Health) ?? 0f,
                    self.Attributes?.GetCurrentValue(AttrType.MaxHealth) ?? 0f);
            }

            if (attribute.AttrType == AttrType.Health)
            {
                if (after < before)
                {
                    self.DispatchGameplayEvent(GameplayEventType.OnTakeDamage);
                }
            }
        }

        public static void DispatchGameplayEvent(this AbilitySystemComponent self, GameplayEventType gameplayEventType)
        {
            AbilityContainerComponent abilityContainer = self.Abilities;
            if (abilityContainer == null)
            {
                return;
            }

            using ListComponent<EntityRef<GameplayAbilitySpec>> activeAbilities = ListComponent<EntityRef<GameplayAbilitySpec>>.Create();
            activeAbilities.AddRange(abilityContainer.GetActiveAbilities());

#if UNITY_EDITOR
            bool shouldLog = false;
            for (int i = 0; i < activeAbilities.Count; i++)
            {
                GameplayAbilitySpec diagAbility = activeAbilities[i].As();
                if (diagAbility != null && diagAbility.SkillId == "1010")
                {
                    shouldLog = true;
                    break;
                }
            }

            if (shouldLog)
            {
            }
#endif

            for (int i = activeAbilities.Count - 1; i >= 0; i--)
            {
                GameplayAbilitySpec ability = activeAbilities[i].As();
                if (ability == null || !ability.IsActive)
                {
                    continue;
                }

#if UNITY_EDITOR
                if (ability.SkillId == "1010")
                {
                }
#endif

                ability.OnGameplayEvent(gameplayEventType);
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
            SkillHudManager.Instance?.UnregisterUnit(self);
            AttributeSetContainer attributes = self.Attributes;
            if (attributes != null)
            {
                attributes.OnAnyAttributeChanged -= self.OnAnyAttributeChanged;
            }
            self.OwnedTags?.Clear();
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

            AbilitySystemComponent resolvedTarget = target;
            if (resolvedTarget == null && spec.RequiresMainTarget())
            {
                resolvedTarget = self.FindDefaultMainTarget();
#if UNITY_EDITOR
                if (spec.SkillId == "1010")
                {
                }
#endif
                if (resolvedTarget == null)
                {
#if UNITY_EDITOR
                    if (spec.SkillId == "1010")
                    {
                    }
#endif
                    return false;
                }
            }

            bool success = self.Abilities?.TryActivateAbility(self, spec, resolvedTarget) ?? false;
            if (success)
            {
                EventSystem.Instance.Publish(self.Root(), new AbilitySystemComponent.OnAbilityActivated()
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
            EventSystem.Instance.Publish(self.Root(), new AbilitySystemComponent.OnAbilityEnded()
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

        private static bool RequiresMainTarget(this GameplayAbilitySpec self)
        {
            SkillData graphData = self?.GraphData;
            if (graphData?.nodes == null)
            {
                return false;
            }

            foreach (NodeData node in graphData.nodes)
            {
                if (node != null && node.targetType == TargetType.MainTarget)
                {
                    return true;
                }
            }

            return false;
        }

        private static AbilitySystemComponent FindDefaultMainTarget(this AbilitySystemComponent self)
        {
            SkillUnit skillUnit = self.GetParent<SkillUnit>();
            Unit selfUnit = skillUnit?.Unit.As();
            Scene currentScene = self.Root()?.CurrentScene();
            UnitComponent unitComponent = currentScene?.GetComponent<UnitComponent>();
            if (selfUnit == null || unitComponent?.Children == null)
            {
                return null;
            }

            UnityEngine.Vector3 selfPosition = GetWorldPosition(selfUnit, self);
            AbilitySystemComponent nearestTarget = null;
            float nearestDistanceSqr = float.MaxValue;
            foreach (Entity entity in unitComponent.Children.Values)
            {
                if (entity is not Unit unit || unit.Id == selfUnit.Id)
                {
                    continue;
                }

                if ((UnitType)unit.Config().Type != UnitType.Monster)
                {
                    continue;
                }

                AbilitySystemComponent targetAsc = unit.GetComponent<SkillUnit>()?.ASC.As();
                if (targetAsc == null)
                {
                    continue;
                }

                float? health = targetAsc.Attributes?.GetCurrentValue(AttrType.Health);
                if (health.HasValue && health.Value <= 0f)
                {
                    continue;
                }

                UnityEngine.Vector3 targetPosition = GetWorldPosition(unit, targetAsc);
                float deltaX = targetPosition.x - selfPosition.x;
                float deltaY = targetPosition.y - selfPosition.y;
                float distanceSqr = deltaX * deltaX + deltaY * deltaY;
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestDistanceSqr = distanceSqr;
                    nearestTarget = targetAsc;
                }
            }

            return nearestTarget;
        }

        private static UnityEngine.Vector3 GetWorldPosition(Unit unit, AbilitySystemComponent asc)
        {
            if (asc?.Owner != null)
            {
                return asc.Owner.transform.position;
            }

            return unit == null
                ? UnityEngine.Vector3.zero
                : new UnityEngine.Vector3(unit.Position.x, unit.Position.y, unit.Position.z);
        }

        private static string DescribeTarget(AbilitySystemComponent target)
        {
            if (target == null)
            {
                return "null";
            }

            Unit unit = target.GetParent<SkillUnit>()?.Unit.As();
            UnityEngine.Vector3 position = GetWorldPosition(unit, target);
            return $"cfg={unit?.ConfigId ?? 0} id={unit?.Id ?? 0} pos={position}";
        }
    }
}
