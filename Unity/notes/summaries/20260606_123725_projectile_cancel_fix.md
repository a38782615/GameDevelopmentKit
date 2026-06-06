# ProjectileEffect 取消日志修复

## 问题

- `ProjectileEffect` 视图实体异步加载过程中，投射物逻辑可能已经命中、到达或被取消。
- `CancelProjectileView()` 会释放 `UGFEntityProjectile` 并取消 `ShowEntityAsync` 的 Token，原逻辑把 `OperationCanceledException` 当成生成失败打印 Error。
- `UGFEntity.ShowEntityAsync` 在 await 抛异常时没有执行 `FreeToken()`，取消路径存在令牌引用计数未释放风险。

## 修复

- `ProjectileEffectSpecHandler` 单独捕获 `OperationCanceledException`，按主动取消处理并静默退出，不再输出 `Spawn projectile failed` Error。
- `UGFEntity` 的实体显示异步流程改为本地持有取消源和 Token，并在 `finally` 中释放 Token。
- `UGFEntity.Dispose()` 在异步显示未结束时只取消 Token，不提前回收取消源，等待 await 结束后再回收。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
