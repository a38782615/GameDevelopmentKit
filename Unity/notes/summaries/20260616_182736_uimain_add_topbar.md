# UIMain 添加 TopBar

本次处理将 `TopBar` 以 ET UIWidget 的方式接入 `UIMain`。

- `UIMain` 的 `MonoUIFormMain.Bind.cs` 已生成 `TopBarTopBar` 绑定字段。
- `UIMain.prefab` 已绑定 `m_TopBarTopBar`，并保留 `TopBar_TopBar` 子节点。
- `MonoUIWidgetTopBar` 增加了 `[CodeBindName("TopBar")]`，保证父级 CodeBind 能正确识别该 widget。
- 新增 `ET/GenAtom/Finalize UIMain Prefab` 编辑器工具，用于后续自动补齐 `TopBar`、生成绑定并刷新序列化。
- 使用 AIBridge 执行 Unity 编译检查，编译通过且无错误。
