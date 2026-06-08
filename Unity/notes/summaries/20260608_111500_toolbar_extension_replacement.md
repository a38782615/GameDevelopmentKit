# ToolbarExtension 替换记录

## 问题

- Unity 6000.3.17f1 下项目内所有基于 `ToolbarExtension` 的自定义 Toolbar 按钮均不生效。
- 原因定位为第三方 `me.xw.toolbarextension` 包在当前 Unity 版本的工具栏挂接链路失效，导致所有 `[Toolbar]` 回调没有正常显示。

## 处理

- 删除 `Packages/manifest.json` 中的 `me.xw.toolbarextension` 依赖。
- 删除 `Packages/packages-lock.json` 中对应锁定项。
- 在 `Assets/Scripts/Game/Editor/ToolbarExtension/` 下新增项目内自维护的 `ToolbarExtension.Editor` 程序集。
- 保留原有 `ToolbarExtension` 命名空间、`ToolbarAttribute`、`OnGUISide` API，避免现有业务 Toolbar 脚本逐个改造。
- 新实现使用 `TypeCache.GetMethodsWithAttribute<ToolbarAttribute>()` 收集所有静态 Toolbar 回调，并通过反射挂接到 Unity 主工具栏的 `ToolbarZoneLeftAlign` / `ToolbarZoneRightAlign`。

## 验证

- 已执行 AIBridge `focus --raw`。
- 已执行 AIBridge `editor stop --raw`。
- 已执行 AIBridge `asset refresh --raw`。
- 已执行 AIBridge `compile unity --raw --timeout 120000`，结果成功，`errorCount = 0`，`warningCount = 0`。

## 说明

- Console 中出现过一次 `ToolbarExtension.Editor` 重名警告，但当时是包依赖移除过程中的旧日志残留。
- 当前 `Packages` 目录下已不存在 `me.xw.toolbarextension` 目录，项目仅保留本地 `ToolbarExtension.Editor` 实现。
