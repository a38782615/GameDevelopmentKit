using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class SkillCellComponentSystem
    {
        private const float StateRefreshInterval = 0.1f;

        [EntitySystem]
        private static void Awake(this SkillCellComponent self, MonoUISkillItem view)
        {
            self.View = view;
            self.StateRefreshLeftTime = 0f;
            self.StateInitialized = false;
        }

        [EntitySystem]
        private static void Update(this SkillCellComponent self)
        {
            if (self.View == null || !self.View.gameObject.activeInHierarchy)
            {
                return;
            }

            self.StateRefreshLeftTime -= Time.deltaTime;
            if (self.StateRefreshLeftTime > 0f)
            {
                return;
            }

            self.StateRefreshLeftTime = StateRefreshInterval;
            self.RefreshState();
        }

        [EntitySystem]
        private static void Destroy(this SkillCellComponent self)
        {
            if (self.View?.CastButton != null)
            {
                self.View.CastButton.onClick.RemoveAllListeners();
            }

            self.View = null;
            self.Spec = default;
            self.CachedStateText = null;
        }

        public static void Bind(this SkillCellComponent self, GameplayAbilitySpec spec)
        {
            if (self.View == null || spec == null)
            {
                return;
            }

            bool specChanged = self.Spec.As() != spec;
            self.Spec = spec;

            if (specChanged)
            {
                global::ET.DRSkill skillData = self.GetSkillData(spec);
                string skillLabel = skillData?.Name ?? spec.SkillId;
                string iconPath = skillData?.IconPath;

                if (self.View.NameText != null)
                {
                    self.View.NameText.text = skillLabel;
                }

                self.View.SetIcon(iconPath);
                self.View.CastButton?.Set(() => self.OnClickCastButton());
                self.StateInitialized = false;
            }

            self.StateRefreshLeftTime = 0f;
            self.RefreshState();
        }

        private static void OnClickCastButton(this SkillCellComponent self)
        {
            GameplayAbilitySpec spec = self.Spec.As();
            if (spec == null)
            {
                return;
            }

            self.TryCastSkill(spec);
            self.StateRefreshLeftTime = 0f;
            self.RefreshState();
        }

        private static void RefreshState(this SkillCellComponent self)
        {
            MonoUISkillItem view = self.View;
            GameplayAbilitySpec spec = self.Spec.As();
            if (view == null || spec == null)
            {
                return;
            }

            if (self.TryApplyEditorOverride(spec))
            {
                return;
            }

            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            bool canCast = !spec.IsActive && !cooldownInfo.IsOnCooldown && spec.CanAffordCost();
            string stateText = self.GetStateText(spec, cooldownInfo);

            if (!self.StateInitialized || self.CachedCanCast != canCast)
            {
                if (view.CastButton != null)
                {
                    view.CastButton.interactable = canCast;
                }

                self.CachedCanCast = canCast;
            }

            if (!self.StateInitialized || self.CachedStateText != stateText)
            {
                if (view.StateText != null)
                {
                    view.StateText.text = stateText;
                }

                self.CachedStateText = stateText;
            }

            self.StateInitialized = true;
        }

        private static bool TryApplyEditorOverride(this SkillCellComponent self, GameplayAbilitySpec spec)
        {
#if UNITY_EDITOR
            UIFormSkillComponent owner = self.Owner.As();
            if (owner == null ||
                owner.EditorSmokeStateOverrideLeftTime <= 0f ||
                owner.EditorSmokeSpec.As() != spec ||
                string.IsNullOrEmpty(owner.EditorSmokeStateOverrideText))
            {
                return false;
            }

            if (self.View.CastButton != null)
            {
                self.View.CastButton.interactable = true;
            }

            if (self.View.StateText != null && self.CachedStateText != owner.EditorSmokeStateOverrideText)
            {
                self.View.StateText.text = owner.EditorSmokeStateOverrideText;
            }

            self.CachedCanCast = true;
            self.CachedStateText = owner.EditorSmokeStateOverrideText;
            self.StateInitialized = true;
            return true;
#else
            return false;
#endif
        }

        private static string GetStateText(this SkillCellComponent self, GameplayAbilitySpec spec, SkillCooldownInfo cooldownInfo)
        {
            if (spec.IsActive)
            {
                return "Casting";
            }

            if (cooldownInfo.IsOnCooldown)
            {
                if (cooldownInfo.IsChargeCooldown)
                {
                    return $"{cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
                }

                return $"CD {cooldownInfo.RemainingTime:0.0}";
            }

            return "Ready";
        }

        private static bool TryCastSkill(this SkillCellComponent self, GameplayAbilitySpec spec)
        {
            AbilitySystemComponent asc = spec?.GetASC;
            if (spec == null || asc == null)
            {
                return false;
            }

            return asc.TryActivateAbility(spec);
        }

        private static global::ET.DRSkill GetSkillData(this SkillCellComponent self, GameplayAbilitySpec spec)
        {
            int skillId = spec.AbilityNodeData?.skillId ?? 0;
            if (skillId <= 0 && !int.TryParse(spec.SkillId, out skillId))
            {
                return null;
            }

            return Tables.Instance.DTSkill.GetOrDefault(skillId);
        }
    }
}
