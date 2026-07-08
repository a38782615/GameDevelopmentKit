using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(UIFormBag1))]
    [EntitySystemOf(typeof(UIFormBag1))]
    [FriendOfAttribute(typeof(ET.Client.GameDataMgrComponent))]
    [FriendOfAttribute(typeof(ET.Client.InventoryDataComponent))]
    public static partial class UIFormBag1System
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormBag1 self)
        {
            self.OpenAllUIWidgets();
            self.BindMapSwitchButtons();
            self.LoadGrid();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormBag1 self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
        }
        private static void BindMapSwitchButtons(this UIFormBag1 self)
        {
            self.View.RetunExButton.SetAsync(self.Return);
        }

        private static void UnbindMapSwitchButtons(this UIFormBag1 self)
        {
            self.View?.RetunExButton?.onClick.RemoveAllListeners();
        }

        private static async UniTask Return(this UIFormBag1 self, Button button)
        {
            var root = self.Root();
            await EventSystem.Instance.PublishAsync(root, new GoScene()
            {
                SceneId = Tables.Instance.DTGameConfig.SceneMain,
                UI = UGFUIFormId.UIMain
            });
        }

        private static void LoadGrid(this UIFormBag1 self)
        {
            self.View.Grid0CommonLoopScrollRect.itemRenderer = self.DropItemRender;
            self.View.Grid0CommonLoopScrollRect.numItems = self.Root().GetInventoryDataComponent().Drops.Count;

            self.View.Grid1CommonLoopScrollRect.itemRenderer = self.BagItemRender;
            self.View.Grid1CommonLoopScrollRect.numItems = self.Root().GetInventoryDataComponent().Items.Count;
        }

        private static void DropItemRender(this UIFormBag1 self, int idx, Transform transform)
        {
            
        }

        private static void BagItemRender(this UIFormBag1 self, int idx, Transform transform)
        {

        }
    }
}