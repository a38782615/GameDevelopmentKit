using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [FriendOf(typeof(UIWidgetBtmBar))]
    [EntitySystemOf(typeof(UIWidgetBtmBar))]
    public static partial class UIWidgetBtmBarSystem
    {
        [EntitySystem]
        private static void Awake(this UIWidgetBtmBar self)
        {

        }

        [EntitySystem]
        private static void Destroy(this UIWidgetBtmBar self)
        {

        }
        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnOpen(this UIWidgetBtmBar self)
        {
            self.BindButton(self.View?.MapExButton, GameConst.Btm_Map);
            self.BindButton(self.View?.BagExButton, GameConst.Btm_Bag);
            self.BindButton(self.View?.HomeExButton, GameConst.Btm_Home);
            self.BindButton(self.View?.SkillExButton, GameConst.Btm_Skill);
            self.BindButton(self.View?.DoExButton, GameConst.Btm_Do);
            self.BindButton(self.View?.FacExButton, GameConst.Btm_Fac);
        }

        [UGFUIWidgetSystem]
        private static void UGFUIWidgetOnClose(this UIWidgetBtmBar self, bool isShutdown)
        {
            self.UnbindButton(self.View?.BagExButton);
            self.UnbindButton(self.View?.MapExButton);
            self.UnbindButton(self.View?.HomeExButton);
            self.UnbindButton(self.View?.SkillExButton);
            self.UnbindButton(self.View?.DoExButton);
            self.UnbindButton(self.View?.FacExButton);
        }

        private static void BindButton(this UIWidgetBtmBar self, UnityEngine.UI.Button button, string actionName)
        {
            if (button == null)
            {
                return;
            }

            button.SetAsync(async () => await self.OnBtmBarButtonClickAsync(actionName));
        }

        private static void UnbindButton(this UIWidgetBtmBar self, UnityEngine.UI.Button button)
        {
            button?.onClick.RemoveAllListeners();
        }

        private static async UniTask OnBtmBarButtonClickAsync(this UIWidgetBtmBar self, string actionName)
        {
            Log.Info($"[UIMain] Click {actionName}");

            UGFUIForm owner = self.GetParent<UGFUIForm>();
            if (owner == null)
            {
                return;
            }

            UIComponent uiComponent = owner.Scene()?.GetComponent<UIComponent>();
            if (uiComponent == null)
            {
                return;
            }
            Scene root = owner.Root();
            await UniTask.DelayFrame(2);

            if (actionName == GameConst.Btm_Bag)
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMain
                });
            }
            else
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMain
                });
            }

            owner.Visible = false;
        }
    }
}
