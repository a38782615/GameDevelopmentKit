# 实现 SpriteRendererSet

## 修改内容

- 参考 `UXImageSet` 新增 `SpriteRendererSet`，支持通过 `AssetSetComponent` 加载 `Sprite` 并设置到 `SpriteRenderer.sprite`。
- 新增 `WaitableSpriteRendererSet`，支持 `SpriteRenderer.SetSpriteAsync` 等待资源设置完成。
- 扩展 `AssetSetExtension`，为 `SpriteRenderer` 增加 `SetSprite` 和 `SetSpriteAsync`。
- 将 `GFEntityHeadItemSystem.SetHeadIconAsync` 改为使用 `SpriteRenderer.SetSpriteAsync`，头像设置走统一 AssetSet 资源管理流程。

## 验证

- 已执行 Unity 资源刷新，新增脚本 `.meta` 已生成。
- 已停止 Unity Play 模式。
- 已通过 AIBridge 执行 Unity 编译检查，结果 `success=true`，错误数 `0`，警告数 `0`。
- 本次涉及文件未发现中文乱码特征。
