using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormMain))]
    [FriendOf(typeof(UIFormMain))]
    public static partial class UIFormMainComponentSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormMain self)
        {
            MonoUIWidgetBtmBar btmBarView = self.View?.BtmBarBtmBar;
            self.BindButton(btmBarView?.MapExButton, GameConst.Btm_Map);
            self.BindButton(btmBarView?.BagExButton, GameConst.Btm_Bag);
            self.BindButton(btmBarView?.HomeExButton, GameConst.Btm_Home);
            self.BindButton(btmBarView?.SkillExButton, GameConst.Btm_Skill);
            self.BindButton(btmBarView?.DoExButton, GameConst.Btm_Do);
            self.BindButton(btmBarView?.FacExButton, GameConst.Btm_Fac);
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMain self, bool isShutdown)
        {
            MonoUIWidgetBtmBar btmBarView = self.View?.BtmBarBtmBar;
            self.UnbindButton(btmBarView?.BagExButton);
            self.UnbindButton(btmBarView?.MapExButton);
            self.UnbindButton(btmBarView?.HomeExButton);
            self.UnbindButton(btmBarView?.SkillExButton);
            self.UnbindButton(btmBarView?.DoExButton);
            self.UnbindButton(btmBarView?.FacExButton);
        }

        private static void BindButton(this UIFormMain self, UnityEngine.UI.Button button, string actionName)
        {
            if (button == null)
            {
                return;
            }

            button.SetAsync(async () => await self.OnMainButtonClickAsync(actionName));
        }

        private static void UnbindButton(this UIFormMain self, UnityEngine.UI.Button button)
        {
            button?.onClick.RemoveAllListeners();
        }

        private static async UniTask OnMainButtonClickAsync(this UIFormMain self, string actionName)
        {
            Log.Info($"[UIMain] Click {actionName}");

            UIComponent uiComponent = self.Scene()?.GetComponent<UIComponent>();
            if (uiComponent == null)
            {
                return;
            }

            if (actionName == GameConst.Btm_Bag)
            {
                await UniTask.DelayFrame(2);
                var root = self.Root();
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMapFight
                });

                self.Visible = false;
            }
        }
    }
}
