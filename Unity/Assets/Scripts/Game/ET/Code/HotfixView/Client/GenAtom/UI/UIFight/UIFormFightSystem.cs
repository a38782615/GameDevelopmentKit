using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(UIFormFight))]
    [EntitySystemOf(typeof(UIFormFight))]
    public static partial class UIFormFightSystem
    {
        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormFight self)
        {
            self.OpenAllUIWidgets();
            self.LoadWidgetMapAsync(self.View.CenterRectTransform).Forget();
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormFight self, bool isShutdown)
        {
        }

        private static async UniTask LoadWidgetMapAsync(this UIFormFight self, RectTransform parentRectTransform)
        {
            var uiWidget = await self.LoadChildUIWidgetAsync<UIWidgetMap>(self.Maps[self.CurrentMap]);
            uiWidget.CachedRectTransform.SetParent(parentRectTransform);
            uiWidget.CachedRectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiWidget.CachedRectTransform.localScale = Vector3.one;
            uiWidget.Open();
        }
    }
}
