# Unit 投射物跳过碰撞检测

## 修改

- 移除 Unit 型投射物通过碰撞触发 `OnReachTarget` 的路径。
- 投射物移动后只执行目标位置到达检测，不再调用 `CheckProjectileCollision()`。
- Unit 型投射物到达目标单位位置后触发 `OnReachTarget`，随后销毁投射物逻辑。
- Position 型投射物继续保持只按目标位置到达触发，不依赖碰撞检测。

## 原因

- 到达目标位置不应依赖碰撞检测。
- Unit 目标类型也应以目标单位位置到达作为触发条件，而不是先碰撞再触发。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
