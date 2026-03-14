using System.IO;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(SkillCellComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class SkillCellComponentSystem
    {
        private const float StateRefreshInterval = 0.1f;
        private const string SkillIconCollectionPath = "Assets/Res/UI/UIAtlas/SkillIcon.asset";

        [EntitySystem]
        private static void Awake(this SkillCellComponent self)
        {
            self.StateRefreshLeftTime = 0f;
            self.StateInitialized = false;
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

                self.SetIcon(iconPath);
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
    }
}
