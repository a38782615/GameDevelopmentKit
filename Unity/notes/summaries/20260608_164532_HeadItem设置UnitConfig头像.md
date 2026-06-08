# HeadItem 设置 UnitConfig 头像

## 修改内容

- 在 `GFEntityHeadItemSystem` 中新增 `SetHeadIconAsync`，从 `Unit.Config().HeadIcon` 读取头像配置。
- 按 `Assets/Res/UI/UISprite/Main/head/{HeadIcon}.png` 加载 `Sprite`，并设置到 `HeadItem` 绑定的 `HeadSpriteRenderer.sprite`。
- 在单位视图创建完成后调用头像设置逻辑，确保每个 Unit 创建 HeadItem 时同步应用配置头像。

## 验证

- 已停止 Unity Play 模式。
- 已通过 AIBridge 执行 Unity 编译检查，结果 `success=true`，错误数 `0`，警告数 `0`。
- 本次涉及代码文件未包含中文注释或中文字符串，无乱码修复项。
