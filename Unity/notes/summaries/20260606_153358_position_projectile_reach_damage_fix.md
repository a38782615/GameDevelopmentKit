# Position 投射物到达触发伤害修复

## 问题

- 7001 当前投射物类型为 `ProjectileTargetType.Position`，伤害节点连接在 `ProjectileEffect.OnReachTarget`。
- 投射物更新顺序是先执行碰撞检测，再执行到达检测。
- Position 型投射物如果先碰撞到目标，会触发 `OnHit` 后销毁投射物；由于 7001 没有把伤害接在 `OnHit` 上，`OnReachTarget -> DamageEffect` 不会执行。

## 修复

- 新增统一的 `TriggerProjectileReachTarget` 方法，集中设置 `ReachPosition`、写入 `SkillDiagFileLogger` 日志并执行 `OnReachTarget`。
- Position 型投射物碰撞到有效目标时，也按到达目标处理并触发 `OnReachTarget`。
- 碰撞触发到达时使用被碰撞目标创建 `ParentInput` 上下文，保证 `DamageEffect(targetType=ParentInput)` 能拿到目标。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
