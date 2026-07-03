using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIWidgetMap))]
    public static partial class UIWidgetMapSystem
    {
        private const string MapNodeName = "Map";
        private const char StagePointPrefix = 'P';

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
            Transform mapRoot = self.GetMapRoot();
            if (mapRoot == null)
            {
                return;
            }

            foreach (Transform point in mapRoot)
            {
                if (!TryParseSubLevel(point.name, out int subLevel))
                {
                    continue;
                }

                Button button = point.GetComponent<Button>();
                if (button == null)
                {
                    continue;
                }

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
            Transform mapRoot = self.GetMapRoot();
            if (mapRoot == null)
            {
                return;
            }

            foreach (Transform point in mapRoot)
            {
                Button button = point.GetComponent<Button>();
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
