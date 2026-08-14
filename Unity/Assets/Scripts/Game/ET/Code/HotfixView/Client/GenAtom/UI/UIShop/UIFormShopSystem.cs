using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormShop))]
    [EntitySystemOf(typeof(UIFormShop))]
    public static partial class UIFormShopSystem
    {
        [EntitySystem]
        private static void Awake(this UIFormShop self)
        {
        }

        [EntitySystem]
        private static void Destroy(this UIFormShop self)
        {
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormShop self)
        {
            self.OpenWidget(self.View.BtmBarBtmBar) ;
            self.BindTabs();
            self.View.ShopLoopVerticalScrollRect.itemRenderer = self.ShopItemRender;
            self.SelectTab(0);
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormShop self, bool isShutdown)
        {
            self.UnbindTabs();
            self.View.ShopLoopVerticalScrollRect.itemRenderer = null;
            self.View.ShopLoopVerticalScrollRect.numItems = 0;
            self.DisplayItems.Clear();
            self.ItemWidgets.Clear();
        }

        private static void BindTabs(this UIFormShop self)
        {
            self.UnbindTabs();
            self.View.ShopTab0UXToggle.onValueChanged.AddListener(self.OnTab0Changed);
            self.View.ShopTab1UXToggle.onValueChanged.AddListener(self.OnTab1Changed);
            self.View.ShopTab2UXToggle.onValueChanged.AddListener(self.OnTab2Changed);
        }

        private static void UnbindTabs(this UIFormShop self)
        {
            if (object.ReferenceEquals(self.View, null))
            {
                return;
            }

            self.View.ShopTab0UXToggle.onValueChanged.RemoveListener(self.OnTab0Changed);
            self.View.ShopTab1UXToggle.onValueChanged.RemoveListener(self.OnTab1Changed);
            self.View.ShopTab2UXToggle.onValueChanged.RemoveListener(self.OnTab2Changed);
        }

        private static void OnTab0Changed(this UIFormShop self, bool isOn)
        {
            if (isOn)
            {
                self.RefreshItems(0, ItemType.Medicine);
            }
        }

        private static void OnTab1Changed(this UIFormShop self, bool isOn)
        {
            if (isOn)
            {
                self.RefreshItems(1, null);
            }
        }

        private static void OnTab2Changed(this UIFormShop self, bool isOn)
        {
            if (isOn)
            {
                self.RefreshItems(2, ItemType.Herb);
            }
        }

        private static void SelectTab(this UIFormShop self, int tabIndex)
        {
            self.View.ShopTab0UXToggle.SetIsOnWithoutNotify(tabIndex == 0);
            self.View.ShopTab1UXToggle.SetIsOnWithoutNotify(tabIndex == 1);
            self.View.ShopTab2UXToggle.SetIsOnWithoutNotify(tabIndex == 2);

            if (tabIndex == 0)
            {
                self.RefreshItems(tabIndex, ItemType.Medicine);
            }
            else if (tabIndex == 2)
            {
                self.RefreshItems(tabIndex, ItemType.Herb);
            }
            else
            {
                self.RefreshItems(tabIndex, null);
            }
        }

        private static void RefreshItems(this UIFormShop self, int tabIndex, ItemType? itemType)
        {
            self.DisplayItems.Clear();
            if (itemType.HasValue)
            {
                GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
                ShopItemDataComponent shopItemDataComponent = gameDataMgrComponent?.GetShopItemDataComponent();
                if (shopItemDataComponent != null)
                {
                    self.DisplayItems.AddRange(shopItemDataComponent.GetItems(itemType.Value));
                }
            }

            self.View.ShopLoopVerticalScrollRect.numItems = self.DisplayItems.Count;
            Log.Info(GameFramework.Utility.Text.Format(
                "UIFormShop refreshed, TabIndex={0}, ItemType={1}, Count={2}.",
                tabIndex,
                itemType.HasValue ? ((int)itemType.Value).ToString() : "Pending",
                self.DisplayItems.Count));
        }

        private static void ShopItemRender(this UIFormShop self, int index, Transform transform)
        {
            if (index < 0 || index >= self.DisplayItems.Count)
            {
                return;
            }

            MonoUIShopItem view = transform.GetComponent<MonoUIShopItem>();
            if (view == null)
            {
                Log.Warning(GameFramework.Utility.Text.Format(
                    "UIFormShop item view is missing, Index={0}.", index));
                return;
            }

            var item = new ShopItemTempLogic
            {
                View = view,
                Data = self.DisplayItems[index],
            };
            item.ItemRender();
        }

        private static void ItemRender(this ShopItemTempLogic self)
        {
            DRItems itemConfig = self.Data == null ? null : Tables.Instance.DTItems.GetOrDefault(self.Data.ConfigId);
            self.View.NameUXTextMeshPro.text = itemConfig?.Name ?? string.Empty;
            self.View.CountUXTextMeshPro.text = self.Data == null ? string.Empty : self.Data.Count.ToString();
            self.View.IconImage.enabled = self.Data != null;
        }
    }
}
