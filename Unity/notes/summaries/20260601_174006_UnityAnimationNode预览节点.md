## 本次任务

为技能编辑器新增基于 `UnityEngine.Animation` 的 `UnityAnimationNode`，用于预览预制体上的 legacy `Animation` 节点，并复用现有时间轴能力。

## 主要改动

- 新增 `UnityAnimationNodeData`，保存预制体引用、Animation 组件路径、动画名、时长、循环和时间轴数据。
- 新增 `UnityAnimationNode`，支持选择预制体、选择预制体内 Animation 节点、选择动画片段、播放预览和时间轴拖拽。
- 新增 `UnityAnimationPreviewRenderer`，在编辑器里实例化隐藏预制体并采样 `AnimationClip.SampleAnimation` 渲染预览。
- 扩展 `TimelineView`，让它同时兼容 `AnimationNodeData` 和 `UnityAnimationNodeData`。
- 扩展节点注册、右键菜单、导入导出映射、序列化恢复，以及运行时动画节点解析，使新节点可参与技能运行时链路。

## 验证

- 已执行 `AIBridgeCLI focus --raw`
- 已执行 `AIBridgeCLI editor stop --raw`
- 已执行 `AIBridgeCLI compile unity --raw --timeout 120000`
- 编译结果：成功，`errorCount = 0`

## 说明

- 按要求未执行 git 提交。
- 工作区存在若干与本任务无关的既有改动，本次未处理。
