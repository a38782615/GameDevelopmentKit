# TopBar Age 改为读取 PlayerData

本次处理将 `UITopBar` 中的年龄显示改为直接读取客户端存档数据。

- 在 `UIWidgetTopBarSystem` 的 `UGFUIWidgetOnOpen` 中调用刷新逻辑。
- 通过 `Root -> GameDataMgrComponent -> PlayerDataComponent -> PlayerData` 读取数据。
- `Age` 文本使用 `PlayerData.Age`。
- 同时补齐了 `Level` 与 `StoneCount`，分别读取 `PlayerData.Level` 和 `PlayerData.Diamond`。
- 使用 AIBridge 执行 Unity 编译检查，编译通过且无错误。
