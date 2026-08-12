using System.Collections.Generic;

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
            self.OwnedTagsRef = self.AddComponent<GameTagsComponent>();
            self.AddComponent<AbilityContainerComponent>();
            self.AddComponent<GameplayEffectContainerComponent>();
            self.AddComponent<GameplayCueContainerComponent>();
            self.Init();
        }

        public static void Init(this AbilitySystemComponent self)
        {
            SkillUnit skillUnit = self.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            if (unit == null)
            {
                self.IsInitialized = false;
                return;
            }

            self.IsInitialized = true;
            self.DeathHandled = false;
        }

        public static void HandleAttributeChanged(this AbilitySystemComponent self, int numericType, float before, float after)
        {
            UnityEngine.GameObject ownerObject = self.GetOwnerObject();
            if (ownerObject != null && (numericType == global::ET.NumericType.Hp || numericType == global::ET.NumericType.MaxHp))
            {
                SkillDiagFileLogger.Log($"[HUD] AttributeChanged asc={self.InstanceId} unit={self.GetParent<SkillUnit>()?.Unit.As()?.Id ?? 0} type={numericType} before={before:F3} after={after:F3} owner={ownerObject.name}");
                SkillHudManager.GetOrCreate().UpdateUnitHealth(
                    self.InstanceId,
                    ownerObject,
                    self.Attributes?.GetValue(global::ET.NumericType.Hp) ?? 0f,
                    self.Attributes?.GetValue(global::ET.NumericType.MaxHp) ?? 0f);
            }

            if (numericType != global::ET.NumericType.Hp)
            {
                return;
            }

            if (after < before)
            {
                self.DispatchGameplayEvent(GameplayEventType.OnTakeDamage);
                if (after > 0f)
                {
                    self.PlayBeAttackPresentation();
                }
            }

            self.TryHandleDeath(before, after);
        }

        public static void TryHandleDeath(this AbilitySystemComponent self, float before, float after, bool fromDamage = false)
        {
            if (self == null || self.DeathHandled || after > 0f || !fromDamage && (before <= 0f || after >= before))
            {
                return;
            }

            self.DeathHandled = true;
            SkillDiagFileLogger.Log($"[Death] asc={self.InstanceId} unit={self.GetParent<SkillUnit>()?.Unit.As()?.Id ?? 0} hpBefore={before:F3} hpAfter={after:F3}");
            self.DispatchGameplayEvent(GameplayEventType.OnDeath);
            self.Abilities?.CancelAllAbilities();
            self.PlayDeathPresentationAndRemove();
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

            for (int i = activeAbilities.Count - 1; i >= 0; i--)
            {
                GameplayAbilitySpec ability = activeAbilities[i].As();
                if (ability == null || !ability.IsActive)
                {
                    continue;
                }

                ability.OnGameplayEvent(gameplayEventType);
            }
        }

        [EntitySystem]
        private static void Update(this AbilitySystemComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this AbilitySystemComponent self)
        {
            SkillHudManager.Instance?.UnregisterUnit(self);
            self.ClearOwnerObject();
            self.OwnedTags?.Clear();
            self.OwnedTagsRef = default;
            self.IsInitialized = false;
        }

        public static GameplayAbilitySpec GrantAbility(this AbilitySystemComponent self, SkillData abilityData)
        {
            if (abilityData == null)
            {
                return null;
            }

            return self.Abilities?.GrantAbility(self, abilityData);
        }

        public static bool RemoveAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec)
        {
            return self.Abilities?.RemoveAbility(spec) ?? false;
        }

        public static bool TryActivateAbility(this AbilitySystemComponent self, GameplayAbilitySpec spec, AbilitySystemComponent target = null)
        {
            if (spec == null)
            {
                return false;
            }

            if (!self.IsAlive())
            {
                SkillDiagFileLogger.Log($"[Death] BlockActivate casterDead asc={self?.InstanceId ?? 0} skillId={spec.GetSkillNumericId()}");
                return false;
            }

            AbilitySystemComponent resolvedTarget = target;
            if (resolvedTarget == null && spec.RequiresMainTarget())
            {
                resolvedTarget = self.FindDefaultMainTarget();
                if (resolvedTarget == null)
                {
                    return false;
                }
            }

            if (resolvedTarget != null && !resolvedTarget.IsAlive())
            {
                SkillDiagFileLogger.Log($"[Death] BlockActivate targetDead caster={self.InstanceId} target={resolvedTarget.InstanceId} skillId={spec.GetSkillNumericId()}");
                return false;
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

        public static bool RemoveActiveEffect(this AbilitySystemComponent self, GameplayEffectSpec effectSpec)
        {
            GameplayEffectContainerComponent container = self.EffectContainer;
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
            GameplayEffectContainerComponent container = self.EffectContainer;
            if (container == null || tags.IsEmpty)
            {
                return 0;
            }

            int removedCount = 0;
            for (int i = container.ActiveEffects.Count - 1; i >= 0; i--)
            {
                GameplayEffectSpec effect = container.ActiveEffects[i].As();
                if (effect != null && effect.Tags.AssetTags.HasAnyTags(tags) && self.RemoveActiveEffect(effect))
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        public static bool IsAlive(this AbilitySystemComponent self)
        {
            if (self == null)
            {
                return false;
            }

            float health = self.Attributes.GetValue(global::ET.NumericType.Hp);
            return health > 0f;
        }

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

        public static bool IsCasting(this AbilitySystemComponent self)
        {
            AbilityContainerComponent abilityContainer = self?.Abilities;
            if (abilityContainer == null)
            {
                return false;
            }

            return abilityContainer.GetActiveAbilities().Count > 0;
        }

        public static bool IsCasting(this AbilitySystemComponent self, GameplayAbilitySpec whitelistSpec)
        {
            if (whitelistSpec == null)
            {
                return self.IsCasting();
            }

            int skillId = GetAbilitySkillId(whitelistSpec);
            if (skillId > 0)
            {
                return self.IsCasting(skillId);
            }

            AbilityContainerComponent abilityContainer = self?.Abilities;
            if (abilityContainer == null)
            {
                return false;
            }

            foreach (EntityRef<GameplayAbilitySpec> activeAbilityRef in abilityContainer.GetActiveAbilities())
            {
                GameplayAbilitySpec activeAbility = activeAbilityRef.As();
                if (activeAbility == whitelistSpec)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsCasting(this AbilitySystemComponent self, params int[] whitelistSkillIds)
        {
            AbilityContainerComponent abilityContainer = self?.Abilities;
            if (abilityContainer == null)
            {
                return false;
            }

            IReadOnlyList<EntityRef<GameplayAbilitySpec>> activeAbilities = abilityContainer.GetActiveAbilities();
            if (activeAbilities.Count == 0)
            {
                return false;
            }

            if (whitelistSkillIds == null || whitelistSkillIds.Length == 0)
            {
                return true;
            }

            foreach (EntityRef<GameplayAbilitySpec> activeAbilityRef in activeAbilities)
            {
                GameplayAbilitySpec activeAbility = activeAbilityRef.As();
                if (activeAbility == null)
                {
                    continue;
                }

                int activeSkillId = GetAbilitySkillId(activeAbility);
                if (activeSkillId <= 0)
                {
                    continue;
                }

                foreach (int whitelistSkillId in whitelistSkillIds)
                {
                    if (whitelistSkillId > 0 && activeSkillId == whitelistSkillId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RequiresMainTarget(this GameplayAbilitySpec self)
        {
            SkillData graphData = self?.GetGraphData();
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
                if (!targetAsc.IsAlive())
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
            UnityEngine.Transform ownerTransform = asc?.GetOwnerTransform();
            if (ownerTransform != null)
            {
                return ownerTransform.position;
            }

            return unit == null
                ? UnityEngine.Vector3.zero
                : new UnityEngine.Vector3(unit.Position.x, unit.Position.y, unit.Position.z);
        }

        private static int GetAbilitySkillId(GameplayAbilitySpec spec)
        {
            return spec == null ? 0 : spec.GetSkillNumericId();
        }
    }
}
