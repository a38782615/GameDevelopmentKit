# PlayerMgrComponent

- 新增 `PlayerMgrComponent`，挂在客户端 `Scene` 上缓存当前玩家的 `PlayerData`。
- 新增 `PlayerMgrComponentSystem`，在登录后从 `ArchiveMgrComponent` 当前存档读取 `PlayerData`。
- 如果存档中不存在 `PlayerData`，自动创建默认数据并写回存档。
- 新增 `LoginFinish_AddPlayerMgrComponent`，登录完成后确保挂载 `ArchiveMgrComponent` 与 `PlayerMgrComponent`，并完成玩家存档加载。
- 使用 AIBridge 执行 `focus`、`editor stop`、`compile unity --raw --timeout 120000`，Unity 编译通过，0 error / 0 warning。
