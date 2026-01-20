# 添加动态加载 UIWidget 用例

## 问题陈述

在登录界面（UILogin）添加第三个动态加载 UIWidget 的用例，演示 `LoadChildUIWidgetAsync` 的使用方式。

## 验收标准

- [x] 在 `MonoUIFormLogin.prefab` 中添加 `Test3RectTransform` 作为新 Widget 的挂载点
- [x] 在 `UIFormLoginComponentSystem.cs` 中添加 `LoadTest3` 方法
- [x] 运行时能正确加载并显示第三个 UIWidgetTest

## 涉及文件

- `Assets/Res/UI/UIForm/Demo/UILogin.prefab` - 添加 RectTransform
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/Demo/UI/UILogin/UIFormLoginComponentSystem.cs` - 添加 LoadTest3 方法

## 参考代码

现有的 `LoadTest2` 方法：
```csharp
private static async UniTaskVoid LoadTest2(this UIFormLoginComponent self)
{
    var uiWidget = await self.LoadChildUIWidgetAsync<UIWidgetTest>(UGFUIEntityId.WidgetTest);
    uiWidget.CachedTransform.SetParent(self.View.Test2RectTransform);
    uiWidget.CachedTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    uiWidget.CachedTransform.localScale = Vector3.one;
    uiWidget.Open();
}
```
