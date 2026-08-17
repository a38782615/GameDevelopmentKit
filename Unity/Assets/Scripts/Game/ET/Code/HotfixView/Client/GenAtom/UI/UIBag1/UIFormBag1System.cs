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
            self.SelectedItem = null;
            self.IsSelling = false;
            self.BindMapSwitchButtons();
            self.LoadGrid();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormBag1 self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
            self.View.Grid0LoopVerticalScrollRect.itemRenderer = null;
            self.View.Grid1LoopVerticalScrollRect.itemRenderer = null;
            self.SelectedItem = null;
            self.IsSelling = false;
        }
        private static void BindMapSwitchButtons(this UIFormBag1 self)
        {
            self.View.RetunExButton.SetAsync(self.Return);
            self.View.SellExButton.SetAsync(self.SellSelectedItem);
        }

        private static void UnbindMapSwitchButtons(this UIFormBag1 self)
        {
            if (object.ReferenceEquals(self.View, null))
            {
                return;
            }

            self.View.RetunExButton.onClick.RemoveAllListeners();
            self.View.SellExButton.onClick.RemoveAllListeners();
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
            self.View.Grid0LoopVerticalScrollRect.itemRenderer = self.DropItemRender;

            self.View.Grid1LoopVerticalScrollRect.itemRenderer = self.BagItemRender;

            self.Refresh();
        }

        private static void Refresh(this UIFormBag1 self)
        {
            InventoryDataComponent inventoryDataComponent = self.Root().GetInventoryDataComponent();
            self.View.Grid0LoopVerticalScrollRect.numItems = inventoryDataComponent.Drops.Count;
            self.View.Grid1LoopVerticalScrollRect.numItems = inventoryDataComponent.Items.Count;
            self.RefreshSellButton();
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
            transform.gameObject.SetActive(true);
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
                self.Bag1.View.Grid0LoopVerticalScrollRect.transform as RectTransform,
                self.Bag1.View.Grid1LoopVerticalScrollRect.transform as RectTransform,
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

        private static UniTask ItemClick(this ItemTempLogic self, Button button)
        {
            if (self.Bag1 == null || self.Bag1.IsDisposed || self.Data == null)
            {
                return UniTask.CompletedTask;
            }

            InventoryDataComponent inventoryDataComponent = self.Bag1.Root().GetInventoryDataComponent();
            self.Bag1.SelectedItem = inventoryDataComponent.Items.Contains(self.Data) ? self.Data : null;
            self.Bag1.RefreshSellButton();
            return UniTask.CompletedTask;
        }

        private static async UniTask SellSelectedItem(this UIFormBag1 self, Button button)
        {
            if (self.IsSelling)
            {
                return;
            }

            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            if (gameDataMgrComponent == null)
            {
                return;
            }

            InventoryDataComponent inventoryDataComponent = gameDataMgrComponent.GetInventoryDataComponent();
            PlayerData playerData = self.Root().GetPlayerData();
            InventoryItemData soldItem = self.SelectedItem;
            if (!inventoryDataComponent.TrySell(
                    soldItem,
                    playerData,
                    out int previousItemCount,
                    out int previousPlayerDiamond,
                    out int previousItemIndex,
                    out bool itemRemoved))
            {
                self.RefreshSellButton();
                return;
            }

            self.IsSelling = true;
            self.RefreshSellButton();
            try
            {
                await gameDataMgrComponent.SaveInventorySaleData(playerData, soldItem, itemRemoved);
            }
            catch
            {
                inventoryDataComponent.RollbackSell(
                    soldItem,
                    playerData,
                    previousItemCount,
                    previousPlayerDiamond,
                    previousItemIndex,
                    itemRemoved);
                throw;
            }
            finally
            {
                if (!self.IsDisposed)
                {
                    self.IsSelling = false;
                    if (itemRemoved)
                    {
                        self.SelectedItem = null;
                    }

                    self.Refresh();
                    self.View.Grid1LoopVerticalScrollRect.Refresh();
                }
            }
        }

        private static void RefreshSellButton(this UIFormBag1 self)
        {
            InventoryDataComponent inventoryDataComponent = self.Root().GetInventoryDataComponent();
            self.View.SellExButton.interactable = !self.IsSelling &&
                    inventoryDataComponent.CanSell(self.SelectedItem);
        }
    }
}
