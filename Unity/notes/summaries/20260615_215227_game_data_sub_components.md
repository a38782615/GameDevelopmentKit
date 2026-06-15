# GameDataMgrComponent 数据子组件

- 将 `GameDataMgrComponent` 改为持有 `PlayerDataComponent` 和 `TaskDataComponent`。
- 新增 `PlayerDataComponent` 管理 `PlayerData` 的加载、保存和默认数据创建。
- 新增 `TaskData` 与 `TaskDataComponent`，用于保存游戏任务状态和进度。
- 新增 `TaskDataComponentSystem`，提供任务状态、任务进度的读取、设置、累加和移除接口。
- `GameDataMgrComponentSystem` 现在只负责确保子组件存在，并调度 `LoadAllData` / `SaveAllData`。
- 使用 AIBridge 执行 `focus`、`editor stop`、`asset refresh`、`compile unity --raw --timeout 120000`，Unity 编译通过，0 error / 0 warning。
