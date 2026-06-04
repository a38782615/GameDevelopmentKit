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
            self.BindButton(self.View.MapExButton, GameConst.Btm_Map);
            self.BindButton(self.View.BagExButton, GameConst.Btm_Bag);
            self.BindButton(self.View.HomeExButton, GameConst.Btm_Home);
            self.BindButton(self.View.SkillExButton, GameConst.Btm_Skill);
            self.BindButton(self.View.DoExButton, GameConst.Btm_Do);
            self.BindButton(self.View.FacExButton, GameConst.Btm_Fac);
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMain self, bool isShutdown)
        {
            self.UnbindButton(self.View?.BagExButton);
            self.UnbindButton(self.View?.MapExButton);
            self.UnbindButton(self.View?.HomeExButton);
            self.UnbindButton(self.View?.SkillExButton);
            self.UnbindButton(self.View?.DoExButton);
            self.UnbindButton(self.View?.FacExButton);
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
