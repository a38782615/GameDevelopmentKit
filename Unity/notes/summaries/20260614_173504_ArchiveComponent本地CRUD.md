# ArchiveComponent 本地 CRUD

## 背景

- 参考 UltraLiteDB 源码和 README 的 `UltraLiteDatabase` / `GetCollection` / `Insert` / `Upsert` / `Delete` / `FindById` API。
- UltraLiteDB 面向 Unity/IL2CPP，移除了线程与文件锁相关逻辑，因此 ET 侧统一通过 `CoroutineLockComponent` 串行化访问。

## 实现

- 新增 `ET.Client.ArchiveComponent`，作为 ET 组件保存数据库路径、密码、锁 key、`BsonMapper` 与 `UltraLiteDatabase` 实例。
- 新增 `ArchiveComponentSystem`，提供 `Insert`、`Update`、`Save/Upsert`、`Query/QueryById/QueryAll`、`Remove`、`Count`、索引和集合管理封装。
- 默认集合名使用 `typeof(T).FullName`，也支持调用方显式传入 collection。
- 默认 `BsonMapper` 开启字段序列化和 null 值序列化，便于普通归档数据落库。
- 针对继承 `ET.Entity` 的归档类型，显式过滤 `Entity` 基类运行时成员，仅保留 `Id` 映射为 `_id`。

## 验证

- 已通过 AIBridge 执行 Unity 编译检查：`compile unity --raw --timeout 120000`。
- 编译结果：0 error，0 warning。
