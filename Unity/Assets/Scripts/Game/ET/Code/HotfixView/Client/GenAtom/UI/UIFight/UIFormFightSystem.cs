using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [FriendOf(typeof(FightComponent))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.OpenAllUIWidgets();
            self.BindMapSwitchButtons();
            FightComponent fightComponent = self.Root().CurrentScene()?.GetComponent<FightComponent>();
            self.RefreshMapSwitchButtons(fightComponent);
            if (fightComponent == null)
            {
                return;
            }

            self.LoadWidgetMapAsync(fightComponent, self.View.CenterRectTransform).Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
            self.UnbindMapSwitchButtons();
            self.RemoveCurrentMapWidget();
            self.IsSwitchingMap = false;
        }

        private static void BindMapSwitchButtons(this UIFormFight self)
        {
            self.View?.PreExButton?.SetAsync(async () => await self.SwitchMapAsync(-1));
            self.View?.NextExButton?.SetAsync(async () => await self.SwitchMapAsync(1));
        }

        private static void UnbindMapSwitchButtons(this UIFormFight self)
        {
            self.View?.PreExButton?.onClick.RemoveAllListeners();
            self.View?.NextExButton?.onClick.RemoveAllListeners();
        }

        private static async UniTask SwitchMapAsync(this UIFormFight self, int delta)
        {
            if (self.IsSwitchingMap)
            {
                return;
            }

            FightComponent fightComponent = self.Root().CurrentScene()?.GetComponent<FightComponent>();
            if (fightComponent == null || self.Maps == null || self.Maps.Length == 0)
            {
                self.RefreshMapSwitchButtons(fightComponent);
                return;
            }

            int nextMap = Mathf.Clamp(fightComponent.CurrentMap + delta, 0, self.Maps.Length - 1);
            if (nextMap == fightComponent.CurrentMap)
            {
                self.RefreshMapSwitchButtons(fightComponent);
                return;
            }

            self.IsSwitchingMap = true;
            try
            {
                fightComponent.CurrentMap = nextMap;
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

        private static void RefreshMapSwitchButtons(this UIFormFight self, FightComponent fightComponent)
        {
            int mapCount = self.Maps?.Length ?? 0;
            int currentMap = fightComponent?.CurrentMap ?? 0;
            bool hasMultipleMaps = fightComponent != null && mapCount > 1;

            self.View?.PreExButton?.gameObject.SetActive(hasMultipleMaps && currentMap > 0);
            self.View?.NextExButton?.gameObject.SetActive(hasMultipleMaps && currentMap < mapCount - 1);
        }

        private static async UniTask LoadWidgetMapAsync(this UIFormFight self, FightComponent fightComponent, RectTransform parentRectTransform)
        {
            if (self.Maps == null || fightComponent.CurrentMap < 0 || fightComponent.CurrentMap >= self.Maps.Length)
            {
                return;
            }

            var uiWidget = await self.LoadChildUIWidgetAsync<UIWidgetMap>(self.Maps[fightComponent.CurrentMap]);
            self.CurrentMapWidget = uiWidget;
            uiWidget.CachedRectTransform.SetParent(parentRectTransform);
            uiWidget.CachedRectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiWidget.CachedRectTransform.localScale = Vector3.one;
            uiWidget.Open();
        }

        private static void RemoveCurrentMapWidget(this UIFormFight self)
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
