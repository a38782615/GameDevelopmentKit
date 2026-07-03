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
            self.StageSubLevels = null;
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
            if (self.StageButtons == null || self.StageSubLevels == null)
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

                int subLevel = self.StageSubLevels[i];
                bool hasStage = fightComponent != null && fightComponent.GetStageConfig(subLevel) != null;
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
                self.StageSubLevels != null &&
                self.StageButtons.Length == self.StageSubLevels.Length)
            {
                return;
            }

            Transform mapRoot = self.GetMapRoot();
            if (mapRoot == null)
            {
                self.StageButtons = null;
                self.StageSubLevels = null;
                return;
            }

            List<ExButton> buttons = new List<ExButton>();
            List<int> subLevels = new List<int>();
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
                subLevels.Add(subLevel);
            }

            self.StageButtons = buttons.ToArray();
            self.StageSubLevels = subLevels.ToArray();
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
