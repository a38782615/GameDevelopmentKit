using Cysharp.Threading.Tasks;
using Game;
using UnityEngine.UI;

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
            self.BindButton(self.View?.MapExButton);
            self.BindButton(self.View?.BagExButton);
            self.BindButton(self.View?.HomeExButton);
            self.BindButton(self.View?.SkillExButton);
            self.BindButton(self.View?.DoExButton);
            self.BindButton(self.View?.FacExButton);
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

        private static void BindButton(this UIWidgetBtmBar self, UnityEngine.UI.Button button)
        {
            if (button == null)
            {
                return;
            }

            button.SetAsync(self.OnBtmBarButtonClickAsync);
        }

        private static void UnbindButton(this UIWidgetBtmBar self, UnityEngine.UI.Button button)
        {
            button?.onClick.RemoveAllListeners();
        }

        private static async UniTask OnBtmBarButtonClickAsync(this UIWidgetBtmBar self, Button button)
        {
            Log.Info($"[UIMain] Click {button.name}");

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

            if (button.name.StartsWith(GameConst.Btm_Bag))
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMain,
                    UI = UGFUIFormId.UIFormBag1
                });
            }
            else if (button.name.StartsWith(GameConst.Btm_Skill))
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMain,
                    UI = UGFUIFormId.UIFormSkills
                });
            }
            else
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMain,
                    UI = UGFUIFormId.UIMain
                });
            }

            owner.Visible = false;
        }
    }
}
