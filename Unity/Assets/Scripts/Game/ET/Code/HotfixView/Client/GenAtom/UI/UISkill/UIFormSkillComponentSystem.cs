using System.Globalization;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(UIFormSkillComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardRuntime))]
    [FriendOf(typeof(GenMap))]
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
        private const float LakeInlandMaskTight = 0.8f;
        private const float LakeInlandMaskDefault = 0.88f;
        private const float LakeInlandMaskWide = 0.92f;

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormSkillComponent self)
        {
            self.ListSyncLeftTime = 0f;
            if (self.View?.ReloadSceneButton != null)
            {
                self.View.ReloadSceneButton.SetAsync(self.ReloadCurrentSceneAsync);
            }

            if (self.View?.RerenderMapButton != null)
            {
                self.View.RerenderMapButton.SetAsync(self.RerenderMapAsync);
            }

            self.SyncMapParameterSelections();
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

            if (self.View?.RerenderMapButton != null)
            {
                self.View.RerenderMapButton.onClick.RemoveAllListeners();
            }

            self.DestroySkillItems();
            self.DisposeSkillCells();
            self.SkillCards.Clear();
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
            SkillCardDeckComponent deck = self.GetPlayerCardDeck();
            if (!self.IsSkillListChanged(deck))
            {
                self.RefreshSkillLayout();
                return;
            }

            self.SkillCards.Clear();
            if (deck != null)
            {
                foreach (long cardInstanceId in deck.HandCardIds)
                {
                    SkillCardRuntime card = deck.GetChild<SkillCardRuntime>(cardInstanceId);
                    if (card == null)
                    {
                        continue;
                    }

                    self.SkillCards.Add(card);
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

        private static async UniTask RerenderMapAsync(this UIFormSkillComponent self)
        {
            if (self.IsRerenderingMap)
            {
                return;
            }

            GenMap genMap = self.GetGenMap();
            if (genMap == null || genMap.IsDisposed)
            {
                return;
            }

            self.IsRerenderingMap = true;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                self.ApplyLakeParameterSelections(genMap);
                await genMap.BuildAsync();
            }
            finally
            {
                stopwatch.Stop();
                self.IsRerenderingMap = false;
                Log.Info($"[nmap][Perf][UI] rerenderTotalMs={stopwatch.ElapsedMilliseconds}");
            }
        }

        private static bool IsSkillListChanged(this UIFormSkillComponent self, SkillCardDeckComponent deck)
        {
            if (deck == null)
            {
                return self.SkillCards.Count > 0;
            }

            if (deck.HandCardIds.Count != self.SkillCards.Count)
            {
                return true;
            }

            for (int i = 0; i < deck.HandCardIds.Count; i++)
            {
                SkillCardRuntime card = deck.GetChild<SkillCardRuntime>(deck.HandCardIds[i]);
                if (card == null || self.SkillCards[i].As() != card)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryCastSkillAtIndex(this UIFormSkillComponent self, int index)
        {
            if (index < 0 || index >= self.SkillCards.Count)
            {
                return false;
            }

            SkillCardRuntime card = self.SkillCards[index].As();
            return self.TryCastSkill(card);
        }

        public static bool TryCastSkill(this UIFormSkillComponent self, SkillCardRuntime card)
        {
            SkillCardDeckComponent deck = card?.GetParent<SkillCardDeckComponent>();
            if (card == null || deck == null)
            {
                return false;
            }

            return deck.TryCastCard(card.CardInstanceId);
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
                if (self.SkillCards.Count > 0)
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

            if (self.SkillCards.Count <= 0)
            {
                return;
            }

            self.EditorSmokeTriggered = true;
            if (self.SkillCards.Count <= 0)
            {
                return;
            }

            SkillCardRuntime card = self.SkillCards[0].As();
            GameplayAbilitySpec spec = card?.SpecRef.As();
            global::ET.DRSkill skillData = self.GetSkillData(card);
            self.EditorSmokeSpec = spec;
            self.EditorSmokeSkillLabel = skillData?.Name ?? card?.SkillId.ToString() ?? "Unknown";

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

            self.EnsureSkillItemCount(self.SkillCards.Count);
            for (int i = 0; i < self.SkillItems.Count; ++i)
            {
                MonoUISkillItem item = self.SkillItems[i];
                if (item == null)
                {
                    continue;
                }

                item.transform.SetSiblingIndex(i + 1);
                SkillCardRuntime card = self.SkillCards[i].As();
                if (card == null)
                {
                    continue;
                }

                SkillCellComponent cell = self.GetOrCreateSkillCell(item);
                cell.Bind(card);
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

        private static global::ET.DRSkill GetSkillData(this UIFormSkillComponent self, SkillCardRuntime card)
        {
            int skillId = card?.SkillId ?? 0;
            if (skillId <= 0)
            {
                return null;
            }

            return Tables.Instance.DTSkill.GetOrDefault(skillId);
        }

        private static SkillCardDeckComponent GetPlayerCardDeck(this UIFormSkillComponent self)
        {
            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(self.Scene());
            return unit?.GetComponent<SkillUnit>()?.SkillCardDeck.As();
        }

        private static GenMap GetGenMap(this UIFormSkillComponent self)
        {
            Scene currentScene = self.Scene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                return null;
            }

            return currentScene.GetComponent<GenMap>();
        }

        private static void SyncMapParameterSelections(this UIFormSkillComponent self)
        {
            MonoUIFormSkill view = self.View;
            GenMap genMap = self.GetGenMap();
            if (object.ReferenceEquals(view, null) || genMap == null)
            {
                return;
            }

            self.SetLakeInlandMaskSelection(self.ResolveNearestOption(genMap.LakeInlandMaskRange, LakeInlandMaskTight, LakeInlandMaskDefault, LakeInlandMaskWide));
            self.SetLakeFloatInput(view.LakeThresholdInputField, genMap.LakeThreshold);
            self.SetLakeFloatInput(view.LakeCarveThresholdInputField, genMap.LakeCarveThreshold);
            self.SetLakeFloatInput(view.LakeCarveStrengthInputField, genMap.LakeCarveStrength);
        }

        private static void ApplyLakeParameterSelections(this UIFormSkillComponent self, GenMap genMap)
        {
            if (genMap == null)
            {
                return;
            }

            genMap.LakeInlandMaskRange = self.GetSelectedLakeInlandMaskValue();
            genMap.LakeThreshold = self.GetLakeFloatInputValue(self.View?.LakeThresholdInputField, genMap.LakeThreshold);
            genMap.LakeCarveThreshold = self.GetLakeFloatInputValue(self.View?.LakeCarveThresholdInputField, genMap.LakeCarveThreshold);
            genMap.LakeCarveStrength = self.GetLakeFloatInputValue(self.View?.LakeCarveStrengthInputField, genMap.LakeCarveStrength);
        }

        private static void SetLakeInlandMaskSelection(this UIFormSkillComponent self, int option)
        {
            MonoUIFormSkill view = self.View;
            if (object.ReferenceEquals(view, null))
            {
                return;
            }

            view.LakeInlandMaskTightToggle?.SetIsOnWithoutNotify(option == 0);
            view.LakeInlandMaskDefaultToggle?.SetIsOnWithoutNotify(option == 1);
            view.LakeInlandMaskWideToggle?.SetIsOnWithoutNotify(option == 2);
        }

        private static float GetSelectedLakeInlandMaskValue(this UIFormSkillComponent self)
        {
            MonoUIFormSkill view = self.View;
            if (view?.LakeInlandMaskWideToggle != null && view.LakeInlandMaskWideToggle.isOn)
            {
                return LakeInlandMaskWide;
            }

            if (view?.LakeInlandMaskTightToggle != null && view.LakeInlandMaskTightToggle.isOn)
            {
                return LakeInlandMaskTight;
            }

            return LakeInlandMaskDefault;
        }

        private static void SetLakeFloatInput(this UIFormSkillComponent self, InputField inputField, float value)
        {
            if (inputField == null)
            {
                return;
            }

            inputField.text = value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static float GetLakeFloatInputValue(this UIFormSkillComponent self, InputField inputField, float fallbackValue)
        {
            if (inputField == null)
            {
                return fallbackValue;
            }

            string text = inputField.text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return fallbackValue;
            }

            if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                return value;
            }

            return float.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ? value : fallbackValue;
        }

        private static int ResolveNearestOption(this UIFormSkillComponent self, float value, float option0, float option1, float option2)
        {
            float distance0 = Mathf.Abs(value - option0);
            float distance1 = Mathf.Abs(value - option1);
            float distance2 = Mathf.Abs(value - option2);
            if (distance0 <= distance1 && distance0 <= distance2)
            {
                return 0;
            }

            if (distance2 < distance1)
            {
                return 2;
            }

            return 1;
        }
    }
}
