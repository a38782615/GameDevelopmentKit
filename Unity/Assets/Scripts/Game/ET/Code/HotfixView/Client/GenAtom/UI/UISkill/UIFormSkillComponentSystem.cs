using Cysharp.Threading.Tasks;
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

            self.ListSyncLeftTime = 0f;
            self.SyncSkillList();
#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagUISkill] open runId={self.EditorSmokeRunId + 1} visible={self.SkillSpecs.Count}");
            self.EditorSmokeRunId++;
            self.EditorSmokeTriggered = false;
            self.EditorSmokeReportLeftTime = -1f;
            self.EditorSmokeResultLogged = false;
            self.EditorSmokeSkillLabel = null;
            self.EditorSmokeStateOverrideText = null;
            self.EditorSmokeStateOverrideLeftTime = 0f;
            self.EditorSmokeSpec = default;
            self.TryStartEditorSmokeAfterOpenAsync().Forget();
#endif
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkillComponent self, bool isShutdown)
        {
            if (self.View?.SkillLoopCommonLoopScrollRect != null)
            {
                self.View.SkillLoopCommonLoopScrollRect.itemRenderer = null;
            }

            self.DisposeSkillCells();
            self.SkillSpecs.Clear();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnUpdate(this UIFormSkillComponent self, float elapseSeconds, float realElapseSeconds)
        {
            self.ListSyncLeftTime -= elapseSeconds;
            if (self.ListSyncLeftTime <= 0f)
            {
                self.ListSyncLeftTime = RefreshInterval;
                self.SyncSkillList();
#if UNITY_EDITOR
                self.TryStartEditorSmokeTest();
#endif
            }

#if UNITY_EDITOR
            self.UpdateEditorSmokeStateOverride(elapseSeconds);
            self.TryReportEditorSmokeResult(elapseSeconds);
#endif
        }

        private static void OnClickCloseButton(this UIFormSkillComponent self)
        {
            self.Dispose();
        }

        private static void SyncSkillList(this UIFormSkillComponent self)
        {
            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            AbilitySystemComponent asc = unit?.GetComponent<SkillUnit>()?.ASC.As();
            var grantedAbilities = asc?.Abilities?.GetGrantedAbilities();
            if (!self.IsSkillListChanged(grantedAbilities))
            {
                return;
            }

            self.SkillSpecs.Clear();
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

            self.View.SkillLoopCommonLoopScrollRect.numItems = self.SkillSpecs.Count;
        }

        private static bool IsSkillListChanged(this UIFormSkillComponent self, System.Collections.Generic.IReadOnlyList<EntityRef<GameplayAbilitySpec>> grantedAbilities)
        {
            int visibleIndex = 0;
            if (grantedAbilities != null)
            {
                foreach (EntityRef<GameplayAbilitySpec> abilityRef in grantedAbilities)
                {
                    GameplayAbilitySpec spec = abilityRef.As();
                    if (spec == null || !self.CanDisplaySkill(spec))
                    {
                        continue;
                    }

                    if (visibleIndex >= self.SkillSpecs.Count || self.SkillSpecs[visibleIndex].As() != spec)
                    {
                        return true;
                    }

                    ++visibleIndex;
                }
            }

            return visibleIndex != self.SkillSpecs.Count;
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

            SkillCellComponent cell = self.GetOrCreateSkillCell(item);
            cell.Bind(spec);
        }

        private static bool TryCastSkillAtIndex(this UIFormSkillComponent self, int index)
        {
            if (index < 0 || index >= self.SkillSpecs.Count)
            {
#if UNITY_EDITOR
                SkillDiagFileLogger.Log($"[DiagUISkill] cast skipped invalid-index={index} visible={self.SkillSpecs.Count}");
#endif
                return false;
            }

            GameplayAbilitySpec spec = self.SkillSpecs[index].As();
            return self.TryCastSkill(spec);
        }

        public static bool TryCastSkill(this UIFormSkillComponent self, GameplayAbilitySpec spec)
        {
            AbilitySystemComponent asc = spec?.GetASC;
            if (spec == null || asc == null)
            {
#if UNITY_EDITOR
                SkillDiagFileLogger.Log($"[DiagUISkill] cast skipped null-spec specNull={(spec == null)} ascNull={(asc == null)}");
#endif
                return false;
            }

#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagUISkill] cast begin skillId={spec.SkillId} state={self.GetEditorDebugState(spec)}");
#endif
            bool success = asc.TryActivateAbility(spec);
#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagUISkill] cast end skillId={spec.SkillId} success={success} state={self.GetEditorDebugState(spec)}");
#endif
            return success;
        }

#if UNITY_EDITOR
        private static async UniTaskVoid TryStartEditorSmokeAfterOpenAsync(this UIFormSkillComponent self)
        {
            int runId = self.EditorSmokeRunId;
            SkillDiagFileLogger.Log($"[DiagUISkill] smoke task start runId={runId}");
            for (int i = 0; i < 60; ++i)
            {
                if (self.IsDisposed || self.EditorSmokeRunId != runId)
                {
                    SkillDiagFileLogger.Log($"[DiagUISkill] smoke task stop runId={runId} disposed={self.IsDisposed} currentRunId={self.EditorSmokeRunId}");
                    return;
                }

                self.SyncSkillList();
                if (self.SkillSpecs.Count > 0)
                {
                    SkillDiagFileLogger.Log($"[DiagUISkill] smoke task ready runId={runId} visible={self.SkillSpecs.Count}");
                    self.TryStartEditorSmokeTest();
                    await UniTask.DelayFrame(60);
                    if (!self.IsDisposed && self.EditorSmokeRunId == runId)
                    {
                        self.TryReportEditorSmokeResult(1f);
                    }
                    return;
                }

                await UniTask.DelayFrame(1);
            }

            if (!self.IsDisposed && self.EditorSmokeRunId == runId)
            {
                SkillDiagFileLogger.Log($"[DiagUISkill] smoke task timeout runId={runId} visible={self.SkillSpecs.Count}");
                Log.Warning($"[UISkillSmoke:{runId}] timeout-empty");
            }
        }

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
            SkillDiagFileLogger.Log($"[DiagUISkill] smoke result runId={self.EditorSmokeRunId} skill={self.EditorSmokeSkillLabel} final={finalState}");
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

        private static SkillCellComponent GetOrCreateSkillCell(this UIFormSkillComponent self, MonoUISkillItem item)
        {
            int instanceId = item.gameObject.GetInstanceID();
            if (self.SkillCellMap.TryGetValue(instanceId, out EntityRef<SkillCellComponent> cellRef))
            {
                SkillCellComponent current = cellRef.As();
                if (current != null)
                {
                    return current;
                }
            }

            SkillCellComponent attachedCell = item.UGFUIWidget as SkillCellComponent;
            if (attachedCell != null)
            {
                self.SkillCellMap[instanceId] = attachedCell;
                attachedCell.TryDynamicOpen();
                return attachedCell;
            }

            SkillCellComponent cell = self.AddChildUIWidgetWithId<SkillCellComponent>(item, instanceId);
            self.SkillCellMap[instanceId] = cell;
            cell.TryDynamicOpen();
            return cell;
        }

        private static void DisposeSkillCells(this UIFormSkillComponent self)
        {
            foreach (EntityRef<SkillCellComponent> cellRef in self.SkillCellMap.Values)
            {
                SkillCellComponent cell = cellRef.As();
                if (cell == null)
                {
                    continue;
                }

                cell.Dispose();
            }

            self.SkillCellMap.Clear();
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
