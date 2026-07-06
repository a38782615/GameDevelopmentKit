using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Client
{
    [FriendOf(typeof(UIFormMap))]
    [EntitySystemOf(typeof(UIFormMap))]
    [FriendOfAttribute(typeof(ET.Client.MapGenComponent))]
    public static partial class UIFormMapSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormMap self)
        {
            self.OpenAllUIWidgets();
            self.BindMapSwitchButtons();
            MapGenComponent fightComponent = self.Root().CurrentScene()?.GetComponent<MapGenComponent>();
            self.RefreshMapSwitchButtons(fightComponent);
            if (fightComponent == null)
            {
                return;
            }

            self.LoadWidgetMapAsync(fightComponent, self.View.CenterRectTransform).Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormMap self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
            self.RemoveCurrentMapWidget();
            self.IsSwitchingMap = false;
        }

        private static void BindMapSwitchButtons(this UIFormMap self)
        {
            self.View.PreExButton.SetAsync( self.SwitchMapAsync);
            self.View.NextExButton.SetAsync(self.SwitchMapAsync);
        }

        private static void UnbindMapSwitchButtons(this UIFormMap self)
        {
            self.View?.PreExButton?.onClick.RemoveAllListeners();
            self.View?.NextExButton?.onClick.RemoveAllListeners();
        }

        private static async UniTask SwitchMapAsync(this UIFormMap self, Button button)
        {
            if (self.IsSwitchingMap)
            {
                return;
            }
            var delta = button == self.View.PreExButton ? - 1 : 1; 

            MapGenComponent fightComponent = self.Root().CurrentScene()?.GetComponent<MapGenComponent>();
            if (fightComponent == null || self.Maps == null || self.Maps.Length == 0)
            {
                self.RefreshMapSwitchButtons(fightComponent);
                return;
            }

            int nextMap = Mathf.Clamp(fightComponent.GetMap0() + delta, 0, self.Maps.Length - 1);
            if (nextMap == fightComponent.GetMap0())
            {
                self.RefreshMapSwitchButtons(fightComponent);
                return;
            }

            self.IsSwitchingMap = true;
            try
            {
                PlayerData playerData = self.Root()?.GetPlayerData();
                playerData.Map0 = nextMap;
                self.RefreshMapSwitchButtons(fightComponent);
                self.RemoveCurrentMapWidget();
                await self.LoadWidgetMapAsync(fightComponent, self.View.CenterRectTransform);
            }
            finally
            {
                self.IsSwitchingMap = false;
                self.RefreshMapSwitchButtons(fightComponent);
            }
        }

        private static void RefreshMapSwitchButtons(this UIFormMap self, MapGenComponent fightComponent)
        {
            int mapCount = self.Maps?.Length ?? 0;
            int currentMap = fightComponent?.GetMap0() ?? 0;
            bool hasMultipleMaps = fightComponent != null && mapCount > 1;

            self.View?.PreExButton?.gameObject.SetActive(hasMultipleMaps && currentMap > 0);
            self.View?.NextExButton?.gameObject.SetActive(hasMultipleMaps && currentMap < mapCount - 1);
        }

        private static async UniTask LoadWidgetMapAsync(this UIFormMap self, MapGenComponent fightComponent, RectTransform parentRectTransform)
        {
            var map0 = fightComponent.GetMap0();
            if (self.Maps == null || map0 < 0 || map0 >= self.Maps.Length)
            {
                return;
            }

            var uiWidget = await self.LoadChildUIWidgetAsync<UIWidgetMap>(self.Maps[map0]);
            self.CurrentMapWidget = uiWidget;
            uiWidget.CachedRectTransform.SetParent(parentRectTransform);
            uiWidget.CachedRectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiWidget.CachedRectTransform.localScale = Vector3.one;
            uiWidget.Open();
        }

        private static void RemoveCurrentMapWidget(this UIFormMap self)
        {
            UIWidgetMap currentMapWidget = self.CurrentMapWidget.As();
            self.CurrentMapWidget = default;
            if (currentMapWidget == null || currentMapWidget.IsDisposed)
            {
                return;
            }

            currentMapWidget.Dispose();
        }
    }
}