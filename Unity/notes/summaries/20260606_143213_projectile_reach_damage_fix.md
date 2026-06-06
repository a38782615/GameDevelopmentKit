# 7001 投射物到达触发伤害修复

## 问题

- 7001 技能资产中 `ProjectileEffect.OnReachTarget` 连接到 `DamageEffect`。
- 运行时 `ProjectileTargetType.Unit` 分支原本不会触发 `OnReachTarget`，只在目标丢失并超过最大距离时销毁。
- 投射物更新顺序是先碰撞、后到达检测；命中目标时只触发 `OnHit`，而 7001 当前没有把伤害接在 `OnHit` 上。

## 修复

- Unit 型投射物接近目标单位位置时触发 `OnReachTarget`，并销毁投射物逻辑。
- Unit 型投射物碰撞到指定目标时也触发 `OnReachTarget`，兼容 7001 当前 `OnReachTarget -> DamageEffect` 连线。
- 使用 `SkillDiagFileLogger.Log(...)` 增加投射物到达、碰撞到达、目标丢失销毁诊断。
- 使用 `SkillDiagFileLogger.Log(...)` 增加伤害节点空目标、空配置、实际扣血、无扣血原因诊断。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
