using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormMain))]
    [FriendOf(typeof(UIFormMain))]
    public static partial class UIFormMainComponentSystem
    {
        private const string GMapActionName = "GMap";
        private const string SMapActionName = "SMap";
        private const string ShopActionName = "Shop";

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormMain self)
        {
            self.OpenAllUIWidgets();
            self.BindButton(self.View?.GMapExButton);
            self.BindButton(self.View?.SMapExButton);
            self.BindButton(self.View?.ShopExButton);
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMain self, bool isShutdown)
        {
            self.UnbindButton(self.View?.GMapExButton);
            self.UnbindButton(self.View?.SMapExButton);
            self.UnbindButton(self.View?.ShopExButton);
        }

        private static void BindButton(this UIFormMain self, UnityEngine.UI.Button button)
        {
            if (button == null)
            {
                return;
            }

            button.SetAsync(self.OnMainButtonClickAsync);
        }

        private static void UnbindButton(this UIFormMain self, UnityEngine.UI.Button button)
        {
            button?.onClick.RemoveAllListeners();
        }

        private static async UniTask OnMainButtonClickAsync(this UIFormMain self, UnityEngine.UI.Button button)
        {
            Log.Info($"[UIMain] Click {button.name}");

            Scene root = self.Root();
            if (button.name.StartsWith(SMapActionName))
            {
                self.OpenMap().Forget();
                // await EventSystem.Instance.PublishAsync(root, new GoScene()
                // {
                //     SceneId = Tables.Instance.DTGameConfig.SceneMapFight
                // });
            }
        }

        private static async UniTask OpenMap(this UIFormMain self)
        {
            var scene = self.Root().CurrentScene();
            UIComponent uiComponent = scene.GetComponent<UIComponent>();
            await uiComponent.AddUIFormComponentAsync<UIFormMap>(UGFUIFormId.UIFormMap);

            self.Dispose();
        }
    }
}
