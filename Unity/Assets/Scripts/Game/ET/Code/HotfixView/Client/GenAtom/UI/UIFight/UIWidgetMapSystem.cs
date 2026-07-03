using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIWidgetMap))]
    [EntitySystemOf(typeof(UIWidgetMap))]
    public static partial class UIWidgetMapSystem
    {
        private const string MapNodeName = "Map";
        private const string StagePointNameFormat = "P{0}";

        [EntitySystem]
        private static void Destroy(this UIWidgetMap self)
        {
            self.StageButtons = null;
            self.StageSubLevels = null;
            self.StageSubLevelsLevel = -1;
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetMap self)
        {
            self.BindStageButtons();
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetMap self, bool isShutdown)
        {
            self.UnbindStageButtons();
        }

        private static void BindStageButtons(this UIWidgetMap self)
        {
            FightComponent fightComponent = self.Scene()?.GetComponent<FightComponent>();
            self.RefreshStageSubLevels(fightComponent);
            self.EnsureStageButtonsCached();
            if (self.StageButtons == null || self.StageSubLevels == null)
            {
                return;
            }

            self.HideAllStageButtons();
            for (int i = 0; i < self.StageSubLevels.Length; i++)
            {
                ExButton button = self.StageButtons[i];
                if (button == null)
                {
                    continue;
                }

                int capturedSubLevel = self.StageSubLevels[i];
                button.gameObject.SetActive(true);
                button.SetAsync(async () => await self.LoadBattleAsync(capturedSubLevel));
            }
        }

        private static void UnbindStageButtons(this UIWidgetMap self)
        {
            if (self.StageButtons == null)
            {
                return;
            }

            foreach (ExButton button in self.StageButtons)
            {
                button?.onClick.RemoveAllListeners();
            }
        }

        private static async UniTask LoadBattleAsync(this UIWidgetMap self, int subLevel)
        {
            FightComponent fightComponent = self.Scene()?.GetComponent<FightComponent>();
            if (fightComponent == null)
            {
                return;
            }

            await fightComponent.LoadBattleAsync(subLevel);
        }

        private static void HideAllStageButtons(this UIWidgetMap self)
        {
            if (self.StageButtons == null)
            {
                return;
            }

            foreach (ExButton button in self.StageButtons)
            {
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        private static void EnsureStageButtonsCached(this UIWidgetMap self)
        {
            if (self.StageSubLevels == null)
            {
                self.StageButtons = null;
                return;
            }

            if (self.StageButtons != null && self.StageButtons.Length == self.StageSubLevels.Length)
            {
                return;
            }

            Transform mapRoot = self.GetMapRoot();
            if (mapRoot == null)
            {
                self.StageButtons = null;
                return;
            }

            ExButton[] buttons = new ExButton[self.StageSubLevels.Length];
            for (int i = 0; i < self.StageSubLevels.Length; i++)
            {
                string stagePointName = StagePointNameFormat.Fmt(self.StageSubLevels[i]);
                Transform stagePoint = mapRoot.Find(stagePointName);
                buttons[i] = stagePoint == null ? null : stagePoint.GetComponent<ExButton>();
            }

            self.StageButtons = buttons;
        }

        private static void RefreshStageSubLevels(this UIWidgetMap self, FightComponent fightComponent)
        {
            if (fightComponent == null)
            {
                self.HideAllStageButtons();
                self.StageButtons = null;
                self.StageSubLevels = null;
                self.StageSubLevelsLevel = -1;
                return;
            }

            int currentLevel = fightComponent.GetCurrentStageLevel();
            if (self.StageSubLevels != null && self.StageSubLevelsLevel == currentLevel)
            {
                return;
            }

            self.HideAllStageButtons();
            self.StageButtons = null;

            List<int> subLevels = new List<int>();
            foreach (DRStages stageConfig in Tables.Instance.DTStages.DataList)
            {
                if (stageConfig.Level == currentLevel)
                {
                    subLevels.Add(stageConfig.SubLevel);
                }
            }

            subLevels.Sort();
            self.StageSubLevels = subLevels.ToArray();
            self.StageSubLevelsLevel = currentLevel;
        }

        private static Transform GetMapRoot(this UIWidgetMap self)
        {
            Transform root = self.View == null ? null : self.View.transform;
            if (root == null)
            {
                return null;
            }

            Transform map = root.Find(MapNodeName);
            return map != null ? map : root;
        }

    }
}
