using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class UIFormSkillComponentSystem
    {
        private const float RefreshInterval = 0.2f;

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormSkillComponent self)
        {
            if (self.View.CloseButton != null)
            {
                self.View.CloseButton.Set(self.OnClickCloseButton);
            }

            if (self.View.SkillLoopCommonLoopScrollRect != null)
            {
                self.View.SkillLoopCommonLoopScrollRect.itemRenderer = self.RenderSkillItem;
            }

            self.RefreshLeftTime = 0f;
            self.RefreshSkillList();
#if UNITY_EDITOR
            self.EditorSmokeRunId = 0;
            self.EditorSmokeTriggered = false;
            self.EditorSmokeReportLeftTime = -1f;
            self.EditorSmokeResultLogged = false;
            self.EditorSmokeSkillLabel = null;
            self.EditorSmokeStateOverrideText = null;
            self.EditorSmokeStateOverrideLeftTime = 0f;
            self.EditorSmokeSpec = default;
#endif
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkillComponent self, bool isShutdown)
        {
            if (self.View?.SkillLoopCommonLoopScrollRect != null)
            {
                self.View.SkillLoopCommonLoopScrollRect.itemRenderer = null;
            }

            self.SkillSpecs.Clear();
        }

        private static void OnClickCloseButton(this UIFormSkillComponent self)
        {
            self.Dispose();
        }

        private static void RefreshSkillList(this UIFormSkillComponent self)
        {
            int previousCount = self.SkillSpecs.Count;
            self.SkillSpecs.Clear();

            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            AbilitySystemComponent asc = unit?.GetComponent<SkillUnit>()?.ASC.As();
            var grantedAbilities = asc?.Abilities?.GetGrantedAbilities();
            if (grantedAbilities != null)
            {
                foreach (EntityRef<GameplayAbilitySpec> abilityRef in grantedAbilities)
                {
                    GameplayAbilitySpec spec = abilityRef.As();
                    if (spec == null || !self.CanDisplaySkill(spec))
                    {
                        continue;
                    }

                    self.SkillSpecs.Add(spec);
                }
            }

            if (self.View?.SkillLoopCommonLoopScrollRect == null)
            {
                return;
            }

            if (previousCount != self.SkillSpecs.Count)
            {
                self.View.SkillLoopCommonLoopScrollRect.numItems = self.SkillSpecs.Count;
                return;
            }

            self.View.SkillLoopCommonLoopScrollRect.Refresh();
        }

        private static bool CanDisplaySkill(this UIFormSkillComponent self, GameplayAbilitySpec spec)
        {
            AbilityNodeData abilityNodeData = spec.AbilityNodeData;
            if (abilityNodeData == null)
            {
                return false;
            }

            return abilityNodeData.eventOutputPorts == null || abilityNodeData.eventOutputPorts.Count == 0;
        }

        private static void RenderSkillItem(this UIFormSkillComponent self, int index, Transform itemTransform)
        {
            if (index < 0 || index >= self.SkillSpecs.Count)
            {
                return;
            }

            MonoUISkillItem item = itemTransform.GetComponent<MonoUISkillItem>();
            if (item == null)
            {
                return;
            }

            GameplayAbilitySpec spec = self.SkillSpecs[index].As();
            if (spec == null)
            {
                return;
            }

            global::ET.DRSkill skillData = self.GetSkillData(spec);
            if (item.NameText != null)
            {
                item.NameText.text = skillData?.Name ?? spec.SkillId;
            }

            item.SetIcon(skillData?.IconPath);
            self.RefreshSkillItemState(item, spec);
            item.CastButton?.Set(() => self.OnClickSkillItem(index));
        }

        private static void OnClickSkillItem(this UIFormSkillComponent self, int index)
        {
            self.TryCastSkillAtIndex(index);
        }

        private static bool TryCastSkillAtIndex(this UIFormSkillComponent self, int index)
        {
            if (index < 0 || index >= self.SkillSpecs.Count)
            {
                return false;
            }

            GameplayAbilitySpec spec = self.SkillSpecs[index].As();
            AbilitySystemComponent asc = spec?.GetASC;
            if (spec == null || asc == null)
            {
                return false;
            }

            bool success = asc.TryActivateAbility(spec);
            self.View?.SkillLoopCommonLoopScrollRect?.Refresh();
            return success;
        }

#if UNITY_EDITOR
        private static void TryStartEditorSmokeTest(this UIFormSkillComponent self)
        {
            if (self.EditorSmokeTriggered)
            {
                return;
            }

            if (self.SkillSpecs.Count <= 0)
            {
                return;
            }

            self.EditorSmokeTriggered = true;
            Log.Warning($"[UISkillSmoke:{self.EditorSmokeRunId}] visible={self.SkillSpecs.Count}");
            if (self.SkillSpecs.Count <= 0)
            {
                Log.Warning($"[UISkillSmoke:{self.EditorSmokeRunId}] skipped-empty");
                return;
            }

            GameplayAbilitySpec spec = self.SkillSpecs[0].As();
            global::ET.DRSkill skillData = self.GetSkillData(spec);
            self.EditorSmokeSpec = spec;
            self.EditorSmokeSkillLabel = skillData?.Name ?? spec?.SkillId ?? "Unknown";

            string beforeState = self.GetEditorDebugState(spec);
            bool success = self.TryCastSkillAtIndex(0);
            string immediateState = self.GetEditorDebugState(self.EditorSmokeSpec.As());
            self.EditorSmokeReportLeftTime = success ? 0.8f : -1f;
            self.EditorSmokeStateOverrideText = success ? $"Smoke {immediateState}" : "Smoke Fail";
            self.EditorSmokeStateOverrideLeftTime = 2f;

            Log.Warning($"[UISkillSmoke:{self.EditorSmokeRunId}] activate skill={self.EditorSmokeSkillLabel} success={success} before={beforeState} immediate={immediateState}");
        }

        private static void UpdateEditorSmokeStateOverride(this UIFormSkillComponent self, float elapseSeconds)
        {
            if (self.EditorSmokeStateOverrideLeftTime <= 0f)
            {
                return;
            }

            self.EditorSmokeStateOverrideLeftTime -= elapseSeconds;
            if (self.EditorSmokeStateOverrideLeftTime > 0f)
            {
                return;
            }

            self.EditorSmokeStateOverrideText = null;
            self.View?.SkillLoopCommonLoopScrollRect?.Refresh();
        }

        private static void TryReportEditorSmokeResult(this UIFormSkillComponent self, float elapseSeconds)
        {
            if (self.EditorSmokeResultLogged || self.EditorSmokeReportLeftTime < 0f)
            {
                return;
            }

            self.EditorSmokeReportLeftTime -= elapseSeconds;
            if (self.EditorSmokeReportLeftTime > 0f)
            {
                return;
            }

            self.EditorSmokeResultLogged = true;
            GameplayAbilitySpec spec = self.EditorSmokeSpec.As();
            string finalState = self.GetEditorDebugState(spec);
            Log.Warning($"[UISkillSmoke:{self.EditorSmokeRunId}] result skill={self.EditorSmokeSkillLabel} final={finalState}");
        }

        private static string GetEditorDebugState(this UIFormSkillComponent self, GameplayAbilitySpec spec)
        {
            if (spec == null)
            {
                return "null-spec";
            }

            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            if (spec.IsActive)
            {
                return "Casting";
            }

            if (cooldownInfo.IsOnCooldown)
            {
                if (cooldownInfo.IsChargeCooldown)
                {
                    return $"Charge {cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
                }

                return $"CD {cooldownInfo.RemainingTime:0.00}";
            }

            return "Ready";
        }
#endif

        private static void RefreshSkillItemState(this UIFormSkillComponent self, MonoUISkillItem item, GameplayAbilitySpec spec)
        {
#if UNITY_EDITOR
            if (self.EditorSmokeStateOverrideLeftTime > 0f && self.EditorSmokeSpec.As() == spec && !string.IsNullOrEmpty(self.EditorSmokeStateOverrideText))
            {
                if (item.CastButton != null)
                {
                    item.CastButton.interactable = true;
                }

                if (item.StateText != null)
                {
                    item.StateText.text = self.EditorSmokeStateOverrideText;
                }

                return;
            }
#endif
            SkillCooldownInfo cooldownInfo = spec.GetCooldownInfo();
            bool canCast = !spec.IsActive && !cooldownInfo.IsOnCooldown && spec.CanAffordCost();

            if (item.CastButton != null)
            {
                item.CastButton.interactable = canCast;
            }

            if (item.StateText == null)
            {
                return;
            }

            if (spec.IsActive)
            {
                item.StateText.text = "Casting";
                return;
            }

            if (cooldownInfo.IsOnCooldown)
            {
                if (cooldownInfo.IsChargeCooldown)
                {
                    item.StateText.text = $"{cooldownInfo.CurrentCharges}/{cooldownInfo.MaxCharges}";
                }
                else
                {
                    item.StateText.text = $"CD {cooldownInfo.RemainingTime:0.0}";
                }

                return;
            }

            item.StateText.text = "Ready";
        }

        private static global::ET.DRSkill GetSkillData(this UIFormSkillComponent self, GameplayAbilitySpec spec)
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
