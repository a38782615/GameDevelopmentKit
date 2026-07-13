using CodeBind;
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

            self.View.Grid1CommonLoopScrollRect.itemRenderer = self.BagItemRender;

            self.Refresh();
        }

        private static void Refresh(this UIFormBag1 self)
        {
            self.View.Grid0CommonLoopScrollRect.numItems = self.Root().GetInventoryDataComponent().Drops.Count;
            self.View.Grid1CommonLoopScrollRect.numItems = self.Root().GetInventoryDataComponent().Items.Count;
        }

        private static void DropItemRender(this UIFormBag1 self, int idx, Transform transform)
        {
            var item = new ItemTempLogic(); 
            item.transform = transform;
            item.Bag1 = self;

            var v1 = self.Root().GetInventoryDataComponent().Drops[idx];
            item.Data = v1;

            item.ItemRender();
            item.InitializeDrag(true);
        }

        private static void BagItemRender(this UIFormBag1 self, int idx, Transform transform)
        {
            var item = new ItemTempLogic();
            item.transform = transform;
            item.Bag1 = self;

            var v1 = self.Root().GetInventoryDataComponent().Items[idx];
            item.Data = v1;

            item.ItemRender();
            item.InitializeDrag(false);
        }

        private static void ItemRender(this ItemTempLogic self)
        {
            var transform = self.transform;
            var c = transform.Find("Count").GetComponent<UXTextMeshPro>();
            c.text = self.Data.Count.ToString();

            var btn = transform.GetComponent<ExButton>();
            btn.SetAsync(self.ItemClick);
        }

        private static void InitializeDrag(this ItemTempLogic self, bool isDropItem)
        {
            BagItemDragHandler dragHandler = self.transform.GetComponent<BagItemDragHandler>();
            dragHandler.Initialize(
                isDropItem,
                self.Bag1.View.Grid0CommonLoopScrollRect.transform as RectTransform,
                self.Bag1.View.Grid1CommonLoopScrollRect.transform as RectTransform,
                self,
                OnItemDropped);
        }

        private static void OnItemDropped(BagItemDragHandler dragHandler, bool targetIsDrop)
        {
            ItemTempLogic item = dragHandler.UserData as ItemTempLogic;
            if (item == null || item.Bag1 == null || item.Bag1.IsDisposed)
            {
                return;
            }

            InventoryDataComponent inventory = item.Bag1.Root().GetInventoryDataComponent();
            if (targetIsDrop)
            {
                inventory.BagToDrop(item.Data);
            }
            else
            {
                inventory.DropToBag(item.Data);
            }

            item.Bag1.Refresh();
        }

        private static async UniTask ItemClick(this ItemTempLogic self, Button button)
        {
            if(self.Data.Type1==-1)
            {
                self.Bag1.Root().GetInventoryDataComponent().DropToBag(self.Data);
            }
            else
            {
                self.Bag1.Root().GetInventoryDataComponent().BagToDrop(self.Data);
            }

            self.Bag1?.Refresh();
        }
    }
}
