using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(AbilityContainerComponent))]
    [FriendOf(typeof(AbilityContainerComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayAbilitySpec))]

    public static partial class AbilityContainerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this AbilityContainerComponent self)
        {
            self.GrantedAbilities.Clear();
            self.ActiveAbilities.Clear();
            self.PendingRemove.Clear();
            self.IsUpdating = false;
        }

        [EntitySystem]
        private static void Update(this AbilityContainerComponent self)
        {
            self.Tick(UnityEngine.Time.deltaTime);
        }

        [EntitySystem]
        private static void Destroy(this AbilityContainerComponent self)
        {
            self.CancelAllAbilities();
            self.GrantedAbilities.Clear();
            self.ActiveAbilities.Clear();
            self.PendingRemove.Clear();
        }
        // ============ 事件触发 ============

        public static void FireAbilityGranted(this AbilityContainerComponent self, GameplayAbilitySpec spec)
        {
            EventSystem.Instance.Publish(self.Root(), new AbilityContainerComponent.OnAbilityGranted(spec));
        }
        public static void FireAbilityRemoved(this AbilityContainerComponent self, GameplayAbilitySpec spec)
        {
            EventSystem.Instance.Publish(self.Root(), new AbilityContainerComponent.OnAbilityRemoved(spec));
        }

        // ============ 技能管理 ============

        public static GameplayAbilitySpec GrantAbility(this AbilityContainerComponent self, AbilitySystemComponent asc, SkillData graphData)
        {
            if (graphData == null) return null;

            var spec = self.AddChild<GameplayAbilitySpec>();
            spec.InitAbility(graphData, asc);
            self.GrantedAbilities.Add(spec);

            self.FireAbilityGranted(spec);
            return spec;
        }

        public static bool RemoveAbility(this AbilityContainerComponent self, GameplayAbilitySpec spec)
        {
            if (spec == null || !self.GrantedAbilities.Contains(spec))
                return false;

            if (spec.IsActive)
            {
                spec.CancelAbility();
                self.ActiveAbilities.Remove(spec);
            }

            self.GrantedAbilities.Remove(spec);
            self.FireAbilityRemoved(spec);
            spec.Dispose();
            return true;
        }

        public static bool TryActivateAbility(this AbilityContainerComponent self, AbilitySystemComponent asc, GameplayAbilitySpec spec, AbilitySystemComponent target = null)
        {
            if (spec == null || !self.GrantedAbilities.Contains(spec))
                return false;

            if (self.IsAbilityBlocked(spec))
                return false;

            if (spec.ActivateAbility(target))
            {
                self.ActiveAbilities.Add(spec);
                return true;
            }

            return false;
        }

        private static bool IsAbilityBlocked(this AbilityContainerComponent self, GameplayAbilitySpec spec)
        {
            if (spec.Tags.AssetTags.IsEmpty)
                return false;

            foreach (var activeAbility in self.ActiveAbilities)
            {
                if (activeAbility.As().BlocksAbilityWithTags(spec.Tags.AssetTags))
                    return true;
            }

            return false;
        }

        public static void CancelAbility(this AbilityContainerComponent self, GameplayAbilitySpec spec)
        {
            if (spec == null || !spec.IsActive) return;

            spec.CancelAbility();

            if (self.IsUpdating)
                self.PendingRemove.Add(spec);
            else
                self.ActiveAbilities.Remove(spec);
        }

        public static void EndAbility(this AbilityContainerComponent self, GameplayAbilitySpec spec, bool wasCancelled = false)
        {
            if (spec == null || !spec.IsActive) return;

            spec.EndAbility(wasCancelled);

            if (self.IsUpdating)
                self.PendingRemove.Add(spec);
            else
                self.ActiveAbilities.Remove(spec);
        }

        public static int CancelAbilitiesWithTags(this AbilityContainerComponent self, GameplayTagSet tags)
        {
            if (tags.IsEmpty) return 0;

            int cancelledCount = 0;
            for (int i = self.ActiveAbilities.Count - 1; i >= 0; i--)
            {
                var ability = self.ActiveAbilities[i];
                if (ability.As().Tags.AssetTags.HasAnyTags(tags))
                {
                    self.CancelAbility(ability);
                    cancelledCount++;
                }
            }
            return cancelledCount;
        }

        public static void CancelAllAbilities(this AbilityContainerComponent self)
        {
            for (int i = self.ActiveAbilities.Count - 1; i >= 0; i--)
            {
                self.CancelAbility(self.ActiveAbilities[i]);
            }
        }

        // ============ 查询方法 ============

        public static IReadOnlyList<EntityRef<GameplayAbilitySpec>> GetGrantedAbilities(this AbilityContainerComponent self)
        {
            return self.GrantedAbilities;
        }

        public static IReadOnlyList<EntityRef<GameplayAbilitySpec>> GetActiveAbilities(this AbilityContainerComponent self)
        {
            return self.ActiveAbilities;
        }

        public static GameplayAbilitySpec FindAbilityByGraphData(this AbilityContainerComponent self, SkillData graphData)
        {
            foreach (var spec in self.GrantedAbilities)
            {
                if (spec.As().GraphData == graphData)
                    return spec;
            }
            return null;
        }

        public static GameplayAbilitySpec FindAbilityById(this AbilityContainerComponent self, int skillId)
        {
            foreach (var spec in self.GrantedAbilities)
            {
                if (spec.As().AbilityNodeData?.skillId == skillId)
                    return spec;
            }
            return null;
        }

        public static bool HasAbilityWithTag(this AbilityContainerComponent self, GameplayTag tag)
        {
            foreach (var spec in self.GrantedAbilities)
            {
                if (spec.As().Tags.AssetTags.HasTag(tag))
                    return true;
            }
            return false;
        }

        public static bool HasActiveAbilityWithTag(this AbilityContainerComponent self, GameplayTag tag)
        {
            foreach (var spec in self.ActiveAbilities)
            {
                if (spec.As().Tags.AssetTags.HasTag(tag))
                    return true;
            }
            return false;
        }

        // ============ 更新 ============

        public static void Tick(this AbilityContainerComponent self, float deltaTime)
        {
            self.IsUpdating = true;

            for (int i = 0; i < self.ActiveAbilities.Count; i++)
            {
                var ability = self.ActiveAbilities[i];
                ability.As().TickAbility(deltaTime);

                if (!ability.As().IsActive && !self.PendingRemove.Contains(ability))
                {
                    self.PendingRemove.Add(ability);
                }
            }

            self.IsUpdating = false;

            if (self.PendingRemove.Count > 0)
            {
                foreach (var ability in self.PendingRemove)
                {
                    self.ActiveAbilities.Remove(ability);
                }
                self.PendingRemove.Clear();
            }
        }
    }
}
