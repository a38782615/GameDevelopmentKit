using System.IO;
using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(SkillCardRuntime))]
    public static partial class SkillCellComponentSystem
    {
        private const float StateRefreshInterval = 0.1f;
        private const string SkillIconCollectionPath = "Assets/Res/UI/UIAtlas/SkillIcon.asset";

        [EntitySystem]
        private static void Awake(this SkillCellComponent self)
        {
            self.StateRefreshLeftTime = 0f;
            self.StateInitialized = false;
            self.CachedCooldownVisible = false;
            self.CachedCooldownFillAmount = -1f;
            self.CachedIconPath = null;
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this SkillCellComponent self)
        {
            if (self.View?.CastButton != null)
            {
                self.View.CastButton.Set(self.OnClickCastButton);
            }

            self.StateRefreshLeftTime = StateRefreshInterval;
            self.RefreshState();
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnUpdate(this SkillCellComponent self, float elapseSeconds, float realElapseSeconds)
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
        private static void UGFUIWidgetOnClose(this SkillCellComponent self, bool isShutdown)
        {
            if (self.View?.CastButton != null)
            {
                self.View.CastButton.onClick.RemoveAllListeners();
            }

            self.ResetCooldownVisual();
        }

        public static void Bind(this SkillCellComponent self, SkillCardRuntime card)
        {
            if (self.View == null || card == null)
            {
                return;
            }

            bool cardChanged = self.Card.As() != card;
            self.Card = card;

            GameplayAbilitySpec spec = card.SpecRef.As();
            if (spec == null)
            {
                return;
            }

            if (cardChanged)
            {
                global::ET.DRSkill skillData = self.GetSkillData(card);
                string skillLabel = skillData?.Name ?? card.SkillId.ToString();
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

        private static void OnClickCastButton(this SkillCellComponent self)
        {
            SkillCardRuntime card = self.Card.As();
            if (card == null)
            {
                return;
            }

            self.TryCastSkill(card);
            self.StateRefreshLeftTime = 0f;
            self.RefreshState();
        }

        private static void RefreshState(this SkillCellComponent self)
        {
            MonoUISkillItem view = self.View;
            SkillCardRuntime card = self.Card.As();
            GameplayAbilitySpec spec = card?.SpecRef.As();
            AbilitySystemComponent asc = spec?.GetASC;
            if (view == null || card == null || spec == null || asc == null)
            {
                return;
            }

            if (self.TryApplyEditorOverride(spec))
            {
                return;
            }

            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            self.RefreshCooldownVisual(cooldownInfo);
            float currentMp = asc.Attributes?.GetCurrentValue(global::ET.NumericType.Mp) ?? 0f;
            float resolvedCostMp = card.GetResolvedCostMp();
            bool canCast = card.Zone == SkillCardZone.Hand &&
                !spec.IsActive &&
                !cooldownInfo.IsOnCooldown;
            string stateText = self.GetStateText(card, spec, cooldownInfo, currentMp, resolvedCostMp);

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

        private static bool TryApplyEditorOverride(this SkillCellComponent self, GameplayAbilitySpec spec)
        {
#if UNITY_EDITOR
            UIFormSkillComponent owner = self.GetParent<UIFormSkillComponent>();
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

        private static string GetStateText(this SkillCellComponent self, SkillCardRuntime card, GameplayAbilitySpec spec, SkillCooldownInfo cooldownInfo, float currentMp, float resolvedCostMp)
        {
            string triggerText = card.TriggerType == 1 ? "被动" : "主动";
            string resourceText = $"耗能 {resolvedCostMp:0.#} | MP {currentMp:0.#}";
            if (spec.IsActive)
            {
                return $"{triggerText}\n{resourceText}\n施放中";
            }

            if (cooldownInfo.IsChargeCooldown && cooldownInfo.CurrentCharges < cooldownInfo.MaxCharges)
            {
                return $"{triggerText}\n{resourceText}\n充能 {cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
            }

            if (cooldownInfo.IsOnCooldown)
            {
                return $"{triggerText}\n{resourceText}\n冷却 {cooldownInfo.RemainingTime:0.0}";
            }

            if (card.Zone != SkillCardZone.Hand)
            {
                return $"{triggerText}\n{resourceText}\n{self.GetZoneText(card.Zone)}";
            }

            if (currentMp < resolvedCostMp)
            {
                return $"{triggerText}\n{resourceText}\n法力不足";
            }

            return $"{triggerText}\n{resourceText}";
        }

        private static void RefreshCooldownVisual(this SkillCellComponent self, SkillCooldownInfo cooldownInfo)
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

        private static void ResetCooldownVisual(this SkillCellComponent self)
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

        private static bool TryCastSkill(this SkillCellComponent self, SkillCardRuntime card)
        {
            SkillCardDeckComponent deck = card?.GetParent<SkillCardDeckComponent>();
            if (card == null || deck == null)
            {
                return false;
            }

            return deck.TryCastCard(card.CardInstanceId);
        }

        private static global::ET.DRSkill GetSkillData(this SkillCellComponent self, SkillCardRuntime card)
        {
            int skillId = card?.SkillId ?? 0;
            if (skillId <= 0)
            {
                return null;
            }

            return Tables.Instance.DTSkill.GetOrDefault(skillId);
        }

        private static void SetIcon(this SkillCellComponent self, string iconPath)
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
            iconImage.SetSprite(SkillIconCollectionPath, spritePath);
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

        private static string GetZoneText(this SkillCellComponent self, SkillCardZone zone)
        {
            return zone switch
            {
                SkillCardZone.DrawPile => "抽牌区",
                SkillCardZone.Hand => "出牌区",
                SkillCardZone.Ability => "能力区",
                SkillCardZone.DiscardPile => "弃牌区",
                SkillCardZone.Destroyed => "销毁区",
                _ => string.Empty,
            };
        }
    }
}
