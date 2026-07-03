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
            self.BindButton(self.View?.GMapExButton, GMapActionName);
            self.BindButton(self.View?.SMapExButton, SMapActionName);
            self.BindButton(self.View?.ShopExButton, ShopActionName);
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMain self, bool isShutdown)
        {
            self.UnbindButton(self.View?.GMapExButton);
            self.UnbindButton(self.View?.SMapExButton);
            self.UnbindButton(self.View?.ShopExButton);
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

            Scene root = self.Root();
            if (actionName == SMapActionName)
            {
                await EventSystem.Instance.PublishAsync(root, new GoScene()
                {
                    SceneId = Tables.Instance.DTGameConfig.SceneMapFight
                });
            }
        }
    }
}
