## 本次任务

为当前 ET 战斗血条链路补充 `SkillDiagFileLogger` 诊断日志，定位“受击后没有显示血条”的问题。

## 主要改动

1. 在 `AbilitySystemComponentSystem.HandleAttributeChanged` 中记录 `Hp/MaxHp` 数值变化日志。
2. 在 `SkillHudManager` 中补充以下日志：
   - 单位注册
   - 受击后血量更新
   - 血条可见窗口刷新
   - 相机解析结果
   - 头顶偏移计算来源
   - 血条被过滤原因
   - 实际进入绘制的单位数量
3. 保留上一轮血条头顶偏移修正：
   - `head` 节点改为使用相对根节点的高度偏移
   - 无 `head` 时回退到 `Renderer.bounds`
   - 相机优先使用 `GameEntry.Camera.CurrentSceneCamera`

## 验证

- 已执行 AIBridge:
  - `focus --raw`
  - `editor stop --raw`
  - `compile unity --raw --timeout 120000`
- Unity 编译通过，无错误。

## 后续查看位置

- 运行后日志输出目录：
  - `Temp/SkillDiagLogs`
