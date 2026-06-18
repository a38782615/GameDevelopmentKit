using System.IO;
using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIWidgetSkillItem))]
    [FriendOf(typeof(UIWidgetSkillItem))]
    [FriendOf(typeof(UIFormSkill))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class UIWidgetSkillItemSystem
    {
        private const float StateRefreshInterval = 0.1f;
        private const string SkillIconCollectionPath = "Assets/Res/UI/UIAtlas/SkillIcon.asset";

        [EntitySystem]
        private static void Awake(this UIWidgetSkillItem self)
        {
            self.StateRefreshLeftTime = 0f;
            self.StateInitialized = false;
            self.CachedCooldownVisible = false;
            self.CachedCooldownFillAmount = -1f;
            self.CachedIconPath = null;
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetSkillItem self)
        {
            if (self.View?.CastButton != null)
            {
                self.View.CastButton.Set(self.OnClickCastButton);
            }

            self.StateRefreshLeftTime = StateRefreshInterval;
            self.RefreshState();
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnUpdate(this UIWidgetSkillItem self, float elapseSeconds, float realElapseSeconds)
        {
            if (self.View == null || !self.View.gameObject.activeInHierarchy)
            {
                return;
            }

            self.StateRefreshLeftTime -= elapseSeconds;
            if (self.StateRefreshLeftTime > 0f)
            {
                return;
            }

            self.StateRefreshLeftTime = StateRefreshInterval;
            self.RefreshState();
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetSkillItem self, bool isShutdown)
        {
            if (self.View?.CastButton != null)
            {
                self.View.CastButton.onClick.RemoveAllListeners();
            }

            self.ResetCooldownVisual();
        }

        public static void Bind(this UIWidgetSkillItem self, GameplayAbilitySpec spec)
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
                string skillLabel = skillData?.Name ?? spec.GetSkillNumericId().ToString();
                string iconPath = skillData?.IconPath;

                if (self.View.NameText != null)
                {
                    self.View.NameText.text = skillLabel;
                }

                self.SetIcon(iconPath);
                self.StateInitialized = false;
            }

            self.StateRefreshLeftTime = 0f;
            self.RefreshState();
        }

        private static void OnClickCastButton(this UIWidgetSkillItem self)
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

        private static void RefreshState(this UIWidgetSkillItem self)
        {
            MonoUISkillItem view = self.View;
            GameplayAbilitySpec spec = self.Spec.As();
            AbilitySystemComponent asc = spec?.GetASC;
            if (view == null || spec == null || asc == null)
            {
                return;
            }

            if (self.TryApplyEditorOverride(spec))
            {
                return;
            }

            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            self.RefreshCooldownVisual(cooldownInfo);
            float currentMp = asc.Attributes?.GetValue(global::ET.NumericType.Mp) ?? 0f;
            float resolvedCostMp = spec.GetResolvedCostMp();
            bool canCast = !spec.IsActive &&
                !cooldownInfo.IsOnCooldown &&
                currentMp >= resolvedCostMp;
            string stateText = self.GetStateText(spec, cooldownInfo, currentMp, resolvedCostMp);

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
                    view.StateText.gameObject.SetActive(!string.IsNullOrEmpty(stateText));
                }

                self.CachedStateText = stateText;
            }

            self.StateInitialized = true;
        }

        private static bool TryApplyEditorOverride(this UIWidgetSkillItem self, GameplayAbilitySpec spec)
        {
#if UNITY_EDITOR
            UIFormSkill owner = self.GetParent<UIFormSkill>();
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
                self.View.StateText.gameObject.SetActive(!string.IsNullOrEmpty(owner.EditorSmokeStateOverrideText));
            }

            self.CachedCanCast = true;
            self.CachedStateText = owner.EditorSmokeStateOverrideText;
            self.StateInitialized = true;
            return true;
#else
            return false;
#endif
        }

        private static string GetStateText(this UIWidgetSkillItem self, GameplayAbilitySpec spec, SkillCooldownInfo cooldownInfo, float currentMp, float resolvedCostMp)
        {
            string resourceText = $"耗能 {resolvedCostMp:0.#} | MP {currentMp:0.#}";
            if (spec.IsActive)
            {
                return $"{resourceText}\n施放中";
            }

            if (cooldownInfo.IsChargeCooldown && cooldownInfo.CurrentCharges < cooldownInfo.MaxCharges)
            {
                return $"{resourceText}\n充能 {cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
            }

            if (cooldownInfo.IsOnCooldown)
            {
                return $"{resourceText}\n冷却 {cooldownInfo.RemainingTime:0.0}";
            }

            if (currentMp < resolvedCostMp)
            {
                return $"{resourceText}\n法力不足";
            }

            return resourceText;
        }

        private static void RefreshCooldownVisual(this UIWidgetSkillItem self, SkillCooldownInfo cooldownInfo)
        {
            MonoUISkillItem view = self.View;
            if (view == null)
            {
                return;
            }

            bool isVisible = false;
            float fillAmount = 0f;

            if (cooldownInfo != null)
            {
                if (cooldownInfo.IsChargeCooldown && cooldownInfo.MaxCharges > 0)
                {
                    isVisible = cooldownInfo.CurrentCharges < cooldownInfo.MaxCharges;
                    fillAmount = 1f - cooldownInfo.ChargeProgress;
                }
                else if (cooldownInfo.IsOnCooldown)
                {
                    isVisible = true;
                    fillAmount = cooldownInfo.TotalDuration > 0f
                        ? cooldownInfo.RemainingTime / cooldownInfo.TotalDuration
                        : 0f;
                }
            }

            fillAmount = Mathf.Clamp01(fillAmount);
            if (self.CachedCooldownVisible != isVisible)
            {
                if (view.CooldownTrackImage != null)
                {
                    view.CooldownTrackImage.gameObject.SetActive(false);
                }

                if (view.CooldownRingImage != null)
                {
                    view.CooldownRingImage.gameObject.SetActive(isVisible);
                }

                self.CachedCooldownVisible = isVisible;
            }

            if (!isVisible)
            {
                if (view.CooldownRingImage != null && self.CachedCooldownFillAmount != 0f)
                {
                    view.CooldownRingImage.fillAmount = 0f;
                }

                self.CachedCooldownFillAmount = 0f;
                return;
            }

            if (view.CooldownRingImage != null && !Mathf.Approximately(self.CachedCooldownFillAmount, fillAmount))
            {
                view.CooldownRingImage.fillAmount = fillAmount;
                self.CachedCooldownFillAmount = fillAmount;
            }
        }

        private static void ResetCooldownVisual(this UIWidgetSkillItem self)
        {
            MonoUISkillItem view = self.View;
            if (view?.CooldownTrackImage != null)
            {
                view.CooldownTrackImage.gameObject.SetActive(false);
            }

            if (view?.CooldownRingImage != null)
            {
                view.CooldownRingImage.fillAmount = 0f;
                view.CooldownRingImage.gameObject.SetActive(false);
            }

            self.CachedCooldownVisible = false;
            self.CachedCooldownFillAmount = 0f;
        }

        private static bool TryCastSkill(this UIWidgetSkillItem self, GameplayAbilitySpec spec)
        {
            AbilitySystemComponent asc = spec?.GetASC;
            if (asc == null || spec == null)
            {
                return false;
            }

            return asc.TryActivateAbility(spec);
        }

        private static global::ET.DRSkill GetSkillData(this UIWidgetSkillItem self, GameplayAbilitySpec spec)
        {
            int skillId = spec?.GetSkillNumericId() ?? 0;
            if (skillId <= 0)
            {
                return null;
            }

            return Tables.Instance.DTSkill.GetOrDefault(skillId);
        }

        private static float GetResolvedCostMp(this GameplayAbilitySpec spec)
        {
            CostEffectNodeData costNodeData = string.IsNullOrEmpty(spec?.CostNodeGuid)
                ? null
                : SkillDataCenter.Instance.GetNodeData(spec.SkillId, spec.CostNodeGuid) as CostEffectNodeData;
            if (costNodeData?.attributeModifiers == null)
            {
                return 0f;
            }

            float costMp = 0f;
            foreach (AttributeModifierData modData in costNodeData.attributeModifiers)
            {
                AttributeModifier modifier = AttributeModifier.FromData(modData);
                if (modifier.TargetAttrType != global::ET.NumericType.Mp)
                {
                    continue;
                }

                costMp += Mathf.Abs(modifier.CalculateMagnitude(null));
            }

            return costMp;
        }

        private static void SetIcon(this UIWidgetSkillItem self, string iconPath)
        {
            MonoUISkillItem view = self.View;
            var iconImage = view?.IconImage;
            if (iconImage == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                iconImage.enabled = false;
                self.CachedIconPath = null;
                return;
            }

            iconImage.enabled = true;
            if (self.CachedIconPath == iconPath)
            {
                return;
            }

            self.CachedIconPath = iconPath;
            string spritePath = GetSkillIconSpritePath(iconPath);
            iconImage.SetSprite(spritePath);
        }

        private static string GetSkillIconSpritePath(string iconPath)
        {
            string normalizedPath = iconPath.Replace('\\', '/');
            if (normalizedPath.StartsWith("Assets/"))
            {
                return normalizedPath;
            }

            string iconName = Path.GetFileNameWithoutExtension(normalizedPath);
            return $"Assets/Res/UI/UISprite/SkillIcon/{iconName}.png";
        }

    }
}
