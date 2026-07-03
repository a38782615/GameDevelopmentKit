using Cysharp.Threading.Tasks;
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
            uiWidget.CachedRectTransform.SetParent(parentRectTransform);
            uiWidget.CachedRectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiWidget.CachedRectTransform.localScale = Vector3.one;
            uiWidget.Open();
        }
    }
}
