using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    public static partial class UIFormSkillComponentSystem
    {
        private const float RefreshInterval = 0.2f;
        private const float PanelMaxWidth = 820f;
        private const float PanelSideMargin = 24f;
        private const float PanelPadding = 18f;
        private const float SkillCellWidth = 160f;
        private const float SkillCellHeight = 160f;
        private const float SkillCellSpacingX = 20f;
        private const float SkillCellSpacingY = 20f;

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormSkillComponent self)
        {
            self.ListSyncLeftTime = 0f;
            if (self.View?.ReloadSceneButton != null)
            {
                self.View.ReloadSceneButton.SetAsync(self.ReloadCurrentSceneAsync);
            }
            self.SyncSkillList();
            self.RefreshSkillLayout();
#if UNITY_EDITOR
            self.EditorSmokeRunId++;
            self.EditorSmokeTriggered = false;
            self.EditorSmokeReportLeftTime = -1f;
            self.EditorSmokeResultLogged = false;
            self.EditorSmokeSkillLabel = null;
            self.EditorSmokeStateOverrideText = null;
            self.EditorSmokeStateOverrideLeftTime = 0f;
            self.EditorSmokeSpec = default;
            // self.TryStartEditorSmokeAfterOpenAsync().Forget();
#endif
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormSkillComponent self, bool isShutdown)
        {
            if (self.View?.ReloadSceneButton != null)
            {
                self.View.ReloadSceneButton.onClick.RemoveAllListeners();
            }
            self.DestroySkillItems();
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
                // self.TryStartEditorSmokeTest();
#endif
            }

#if UNITY_EDITOR
            self.UpdateEditorSmokeStateOverride(elapseSeconds);
            self.TryReportEditorSmokeResult(elapseSeconds);
#endif

            self.RefreshSkillLayout();
        }

        private static void SyncSkillList(this UIFormSkillComponent self)
        {
            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            AbilitySystemComponent asc = unit?.GetComponent<SkillUnit>()?.ASC.As();
            var grantedAbilities = asc?.Abilities?.GetGrantedAbilities();
            if (!self.IsSkillListChanged(grantedAbilities))
            {
                self.RefreshSkillLayout();
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

            self.RefreshSkillItems();
        }

        private static async UniTask ReloadCurrentSceneAsync(this UIFormSkillComponent self)
        {
            Scene currentScene = self.Scene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                return;
            }

            Scene root = currentScene.Root();
            if (root == null || root.IsDisposed)
            {
                return;
            }

            await SceneChangeHelper.SceneChangeTo2(root, currentScene.Name, currentScene.Id);
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

        private static bool TryCastSkillAtIndex(this UIFormSkillComponent self, int index)
        {
            if (index < 0 || index >= self.SkillSpecs.Count)
            {
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
                return false;
            }

            bool success = asc.TryActivateAbility(spec);
            return success;
        }

#if UNITY_EDITOR
        private static async UniTaskVoid TryStartEditorSmokeAfterOpenAsync(this UIFormSkillComponent self)
        {
            int runId = self.EditorSmokeRunId;
            for (int i = 0; i < 60; ++i)
            {
                if (self.IsDisposed || self.EditorSmokeRunId != runId)
                {
                    return;
                }

                self.SyncSkillList();
                if (self.SkillSpecs.Count > 0)
                {
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
            if (self.SkillSpecs.Count <= 0)
            {
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

        private static void RefreshSkillItems(this UIFormSkillComponent self)
        {
            if (self.View?.ItemTemplateSkillItemTemplate == null || self.View.SkillGridRectTransform == null)
            {
                return;
            }

            self.EnsureSkillItemCount(self.SkillSpecs.Count);
            for (int i = 0; i < self.SkillItems.Count; ++i)
            {
                MonoUISkillItem item = self.SkillItems[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.SetSiblingIndex(i + 1);
                GameplayAbilitySpec spec = self.SkillSpecs[i].As();
                if (spec == null)
                {
                    continue;
                }

                SkillCellComponent cell = self.GetOrCreateSkillCell(item);
                cell.Bind(spec);
            }

            self.RefreshSkillLayout();
        }

        private static void EnsureSkillItemCount(this UIFormSkillComponent self, int targetCount)
        {
            while (self.SkillItems.Count < targetCount)
            {
                self.CreateSkillItem();
            }

            while (self.SkillItems.Count > targetCount)
            {
                self.DestroySkillItem(self.SkillItems.Count - 1);
            }
        }

        private static void CreateSkillItem(this UIFormSkillComponent self)
        {
            MonoUISkillItem template = self.View?.ItemTemplateSkillItemTemplate;
            RectTransform skillGridRectTransform = self.View?.SkillGridRectTransform;
            if (template == null || skillGridRectTransform == null)
            {
                return;
            }

            MonoUISkillItem item = UnityEngine.Object.Instantiate(template, skillGridRectTransform, false);
            self.SkillItems.Add(item);

            SkillCellComponent cell = self.GetOrCreateSkillCell(item);
            cell.TryDynamicOpen();
        }

        private static void DestroySkillItem(this UIFormSkillComponent self, int index)
        {
            if (index < 0 || index >= self.SkillItems.Count)
            {
                return;
            }

            MonoUISkillItem item = self.SkillItems[index];
            self.SkillItems.RemoveAt(index);
            if (item == null)
            {
                return;
            }

            self.DisposeSkillCell(item.gameObject.GetInstanceID());
            UnityEngine.Object.Destroy(item.gameObject);
        }

        private static void DestroySkillItems(this UIFormSkillComponent self)
        {
            for (int i = self.SkillItems.Count - 1; i >= 0; --i)
            {
                self.DestroySkillItem(i);
            }

            self.SkillItems.Clear();
        }

        private static void DisposeSkillCell(this UIFormSkillComponent self, int instanceId)
        {
            if (!self.SkillCellMap.Remove(instanceId, out EntityRef<SkillCellComponent> cellRef))
            {
                return;
            }

            SkillCellComponent cell = cellRef.As();
            cell?.Dispose();
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

        private static void RefreshSkillLayout(this UIFormSkillComponent self)
        {
            MonoUIFormSkill view = self.View;
            RectTransform rootRectTransform = self.CachedTransform as RectTransform;
            if (view?.PanelRectTransform == null ||
                view.SkillGridRectTransform == null ||
                view.SkillGridGridLayoutGroup == null ||
                rootRectTransform == null)
            {
                return;
            }

            float availableWidth = Mathf.Max(
                (PanelPadding * 2f) + SkillCellWidth,
                rootRectTransform.rect.width - (PanelSideMargin * 2f));
            float panelWidth = Mathf.Min(PanelMaxWidth, availableWidth);
            int itemCount = self.SkillItems.Count;
            int columns = self.GetColumnCount(panelWidth, itemCount);
            int rows = itemCount <= 0 ? 1 : Mathf.CeilToInt(itemCount / (float)columns);
            float panelHeight = (PanelPadding * 2f) +
                (rows * SkillCellHeight) +
                (Mathf.Max(0, rows - 1) * SkillCellSpacingY);

            view.PanelRectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);

            GridLayoutGroup gridLayoutGroup = view.SkillGridGridLayoutGroup;
            gridLayoutGroup.cellSize = new Vector2(SkillCellWidth, SkillCellHeight);
            gridLayoutGroup.spacing = new Vector2(SkillCellSpacingX, SkillCellSpacingY);
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = columns;

            LayoutRebuilder.ForceRebuildLayoutImmediate(view.SkillGridRectTransform);
        }

        private static int GetColumnCount(this UIFormSkillComponent self, float panelWidth, int itemCount)
        {
            float contentWidth = Mathf.Max(0f, panelWidth - (PanelPadding * 2f));
            int columns = Mathf.Max(1, Mathf.FloorToInt((contentWidth + SkillCellSpacingX) / (SkillCellWidth + SkillCellSpacingX)));
            if (itemCount > 0)
            {
                columns = Mathf.Min(columns, itemCount);
            }

            return Mathf.Max(1, columns);
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
