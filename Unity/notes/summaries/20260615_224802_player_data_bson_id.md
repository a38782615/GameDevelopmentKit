# PlayerData BsonId

- 为 `PlayerData` 增加 `Id` 字段，并标记 `[BsonId]`。
- `PlayerDataComponent` 使用固定 `PlayerDataId = 1` 查询和保存当前玩家数据。
- 加载时兼容旧的字符串主键 `PlayerData`，命中后迁移到新的 `Id` 主键并删除旧记录。
- 加载日志增加 `Id` 字段。
- 使用 AIBridge 执行 `focus`、`editor stop`、`compile unity --raw --timeout 120000`，Unity 编译通过，0 error / 0 warning。
