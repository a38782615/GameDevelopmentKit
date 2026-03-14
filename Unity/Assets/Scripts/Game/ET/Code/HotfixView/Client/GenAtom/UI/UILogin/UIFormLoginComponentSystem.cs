using System;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(UIFormLoginComponent))]
    [FriendOf(typeof(UIFormLoginComponent))]
    public static partial class UIFormLoginComponentSystem
    {
        private const string TestWidgetLoadedLog = "[UILogin] 所有 TestWidget 加载完成";

        [UGFUIFormSystem]
        private static void UGFUIFormOnOpen(this UIFormLoginComponent self)
        {
            self.OpenAllUIWidgets();
            self.View.LoginButton.SetAsync(self.OnLogin);
            self.ResetTestWidgetLoadState();
            self.LoadAllTestWidgetsAsync().Forget();
            Log.Debug("Login界面OnOpen");
        }

        [UGFUIFormSystem]
        private static void UGFUIFormOnClose(this UIFormLoginComponent self, bool isShutdown)
        {
            self.CancelWaitAllTestWidgetsLoaded();
        }

        public static UniTask OnLogin(this UIFormLoginComponent self)
        {
            return LoginHelper.Login(
                self.Root(),
                self.View.AccountInputField.text,
                self.View.PasswordInputField.text);
        }

        public static UniTask WaitAllTestWidgetsLoadedAsync(this UIFormLoginComponent self)
        {
            if (self.IsAllTestWidgetsLoaded)
            {
                return UniTask.CompletedTask;
            }

            if (self.TestWidgetsLoadException != null)
            {
                return UniTask.FromException(self.TestWidgetsLoadException);
            }

            self.TestWidgetsLoadedTcs ??= AutoResetUniTaskCompletionSourcePlus.Create();
            return self.TestWidgetsLoadedTcs.Task;
        }

        private static void ResetTestWidgetLoadState(this UIFormLoginComponent self)
        {
            self.IsAllTestWidgetsLoaded = false;
            self.TestWidgetsLoadException = null;
            self.TestWidgetsLoadedTcs?.TrySetCanceled();
            self.TestWidgetsLoadedTcs = AutoResetUniTaskCompletionSourcePlus.Create();
        }

        private static void CancelWaitAllTestWidgetsLoaded(this UIFormLoginComponent self)
        {
            self.TestWidgetsLoadedTcs?.TrySetCanceled();
            self.TestWidgetsLoadedTcs = null;
            self.IsAllTestWidgetsLoaded = false;
            self.TestWidgetsLoadException = null;
        }

        private static async UniTaskVoid LoadAllTestWidgetsAsync(this UIFormLoginComponent self)
        {
            try
            {
                await UniTask.WhenAll(
                    self.LoadTestWidgetAsync(self.View.Test1RectTransform),
                    self.LoadTestWidgetAsync(self.View.Test2RectTransform),
                    self.LoadTestWidgetAsync(self.View.Test3RectTransform));

                self.IsAllTestWidgetsLoaded = true;
                self.TestWidgetsLoadException = null;
                self.TestWidgetsLoadedTcs?.TrySetResult();
                self.TestWidgetsLoadedTcs = null;
                Log.Info(TestWidgetLoadedLog);
            }
            catch (Exception e)
            {
                self.IsAllTestWidgetsLoaded = false;
                self.TestWidgetsLoadException = e;
                self.TestWidgetsLoadedTcs?.TrySetException(e);
                self.TestWidgetsLoadedTcs = null;
                Log.Error(e.ToString());
            }
        }

        private static async UniTask LoadTestWidgetAsync(this UIFormLoginComponent self, RectTransform parentRectTransform)
        {
            var uiWidget = await self.LoadChildUIWidgetAsync<UIWidgetTest>(UGFUIEntityId.WidgetTest);
            uiWidget.CachedRectTransform.SetParent(parentRectTransform);
            uiWidget.CachedRectTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            uiWidget.CachedRectTransform.localScale = Vector3.one;
            uiWidget.Open();
        }
    }
}
