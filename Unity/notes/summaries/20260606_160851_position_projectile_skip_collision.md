# Position 投射物跳过碰撞检测

## 修改

- Position 型投射物移动后先执行目标位置到达检测。
- Position 型投射物不再执行 `CheckProjectileCollision()`。
- 移除了 Position 型投射物通过碰撞触发 `OnReachTarget` 的路径。
- Position 型投射物到达目标位置后触发 `OnReachTarget`，随后销毁投射物逻辑。

## 原因

- 到达目标位置不应依赖碰撞检测。
- 7001 的伤害链路连接在 `ProjectileEffect.OnReachTarget`，应由位置到达判定稳定触发。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
