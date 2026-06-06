# 投射物碰撞时触发 OnHit 修复

## 问题

- 7001 当前资源将 `ProjectileEffect.OnHit` 连接到 `DamageEffect`。
- 前序调整让投射物完全不执行 `CheckProjectileCollision()`，导致 `OnHit` 永远不会触发。

## 修复

- 新增 `ShouldCheckProjectileCollision`，当 `OnHit` 有连线或投射物启用反弹时执行碰撞检测。
- 有碰撞语义时先执行 `CheckProjectileCollision()`，保证 `OnHit` 不会被到达目标逻辑抢先销毁。
- `TriggerProjectileHit` 使用 `SkillDiagFileLogger.Log(...)` 记录命中目标、命中位置和 `ParentInput` 上下文。
- 到达目标位置仍由 `CheckProjectileReachTarget()` 独立触发，不依赖碰撞检测。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
