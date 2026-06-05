# 血条渲染路径修复

- 排查结论：`AttributeChanged`、`VisibleWindow`、`DrawUnit` 日志都正常，血条在受击后持续提交绘制，位置投影也在屏幕内，问题收敛到血条 instancing 渲染路径本身。
- 代码调整：
  - `SkillHudManager` 将血条绘制从 `Graphics.DrawMeshInstanced` 改为逐条 `Graphics.DrawMesh`，继续复用现有四边形批次收集与诊断日志。
  - `SkillHudInstancedBillboard.shader` 改为使用普通材质属性 `_HudColor`、`_HudUvRect`，不再依赖 instanced 属性数组。
- 验证结果：执行 `AIBridgeCLI.exe focus --raw`、`editor stop --raw`、`compile unity --raw --timeout 120000`，Unity 编译通过，`errorCount=0`。
- 剩余说明：当前未在自动化里完整重放一遍战斗受击视觉场景，后续可直接在运行时观察血条是否恢复显示。
