using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [FriendOf(typeof(MapGenComponent))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.OpenAllUIWidgets();
            self.BindMapSwitchButtons();
            MapGenComponent fightComponent = self.Root().CurrentScene()?.GetComponent<MapGenComponent>();
            if (fightComponent == null)
            {
                return;
            }

            fightComponent.LoadBattleAsync().Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
        }

        private static void BindMapSwitchButtons(this UIFormFight self)
        {
            self.View.ReturnExButton.SetAsync(self.ReturnMap);
        }
        private static void UnbindMapSwitchButtons(this UIFormFight self)
        {
            self.View?.ReturnExButton?.onClick.RemoveAllListeners();
        }

        private static async UniTask ReturnMap(this UIFormFight self, Button button)
        {
            var root = self.Root();
            EventSystem.Instance.Publish(root, new GoScene()
            {
                SceneId = Tables.Instance.DTGameConfig.SceneMain,
                UI = UGFUIFormId.UIFormMap
            });
            self.Dispose();
        }
    }
}
