# GameDataMgrComponent

- 将 `PlayerMgrComponent` 重构为统一的 `GameDataMgrComponent`，作为客户端 `Scene` 的游戏存档数据入口。
- 新增 `LoadAllData`、`SaveAllData` 通用入口，当前先纳入 `PlayerData` 的加载与保存。
- 登录完成后改为挂载 `GameDataMgrComponent`，并执行一次全部游戏数据加载。
- 保留 `PlayerData` 默认初始化逻辑；若存档缺失，会自动创建并写回。
- 使用 AIBridge 执行 `focus`、`editor stop`、`asset refresh`、`compile unity --raw --timeout 120000`，Unity 编译通过，0 error / 0 warning。
