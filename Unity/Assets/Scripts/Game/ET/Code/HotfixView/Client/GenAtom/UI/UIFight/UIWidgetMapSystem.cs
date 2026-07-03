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
        private const char StagePointPrefix = 'P';

        [EntitySystem]
        private static void Destroy(this UIWidgetMap self)
        {
            self.StageButtons = null;
            self.StageButtonSubLevels = null;
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
            self.EnsureStageButtonsCached();
            FightComponent fightComponent = self.Scene()?.GetComponent<FightComponent>();
            self.RefreshStageSubLevels(fightComponent);
            if (self.StageButtons == null || self.StageButtonSubLevels == null || self.StageSubLevels == null)
            {
                return;
            }

            for (int i = 0; i < self.StageButtons.Length; i++)
            {
                ExButton button = self.StageButtons[i];
                if (button == null)
                {
                    continue;
                }

                int subLevel = self.StageButtonSubLevels[i];
                bool hasStage = self.ContainsStageSubLevel(subLevel);
                button.gameObject.SetActive(hasStage);
                button.onClick.RemoveAllListeners();
                if (!hasStage)
                {
                    continue;
                }

                int capturedSubLevel = subLevel;
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

        private static void EnsureStageButtonsCached(this UIWidgetMap self)
        {
            if (self.StageButtons != null &&
                self.StageButtonSubLevels != null &&
                self.StageButtons.Length == self.StageButtonSubLevels.Length)
            {
                return;
            }

            Transform mapRoot = self.GetMapRoot();
            if (mapRoot == null)
            {
                self.StageButtons = null;
                self.StageButtonSubLevels = null;
                self.StageSubLevels = null;
                self.StageSubLevelsLevel = -1;
                return;
            }

            List<ExButton> buttons = new List<ExButton>();
            List<int> buttonSubLevels = new List<int>();
            foreach (Transform point in mapRoot)
            {
                if (!TryParseSubLevel(point.name, out int subLevel))
                {
                    continue;
                }

                ExButton button = point.GetComponent<ExButton>();
                if (button == null)
                {
                    continue;
                }

                buttons.Add(button);
                buttonSubLevels.Add(subLevel);
            }

            self.StageButtons = buttons.ToArray();
            self.StageButtonSubLevels = buttonSubLevels.ToArray();
        }

        private static void RefreshStageSubLevels(this UIWidgetMap self, FightComponent fightComponent)
        {
            if (fightComponent == null)
            {
                self.StageSubLevels = null;
                self.StageSubLevelsLevel = -1;
                return;
            }

            int currentLevel = fightComponent.GetCurrentStageLevel();
            if (self.StageSubLevels != null && self.StageSubLevelsLevel == currentLevel)
            {
                return;
            }

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

        private static bool ContainsStageSubLevel(this UIWidgetMap self, int subLevel)
        {
            if (self.StageSubLevels == null)
            {
                return false;
            }

            foreach (int stageSubLevel in self.StageSubLevels)
            {
                if (stageSubLevel == subLevel)
                {
                    return true;
                }
            }

            return false;
        }

        private static Transform GetMapRoot(this UIWidgetMap self)
        {
            Transform root = self.View == null ? null : self.View.transform;
            if (root == null)
            {
                return null;
            }

            foreach (Transform child in root)
            {
                if (child.name == MapNodeName)
                {
                    return child;
                }
            }

            return root;
        }

        private static bool TryParseSubLevel(string nodeName, out int subLevel)
        {
            subLevel = 0;
            if (string.IsNullOrEmpty(nodeName) || nodeName[0] != StagePointPrefix || nodeName.Length <= 1)
            {
                return false;
            }

            return int.TryParse(nodeName.Substring(1), out subLevel);
        }

    }
}
