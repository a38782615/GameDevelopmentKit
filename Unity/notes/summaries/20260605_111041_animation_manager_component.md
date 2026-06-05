## 本次任务

实现统一动画管理链路，支持在 `SkelenAnimationComponent` 与新建的 `UnityAnimationComponent` 之间切换，并替换现有直接依赖 `SkelenAnimationComponent` 的调用入口。

## 主要改动

1. 新增 `AnimationDriverType`、`AnimationManagerComponent`、`UnityAnimationComponent`。
2. 新增 `AnimationManagerComponentSystem`，统一处理动画驱动选择、眩晕监听、待机/移动/受控动画播放。
3. 新增 `UnityAnimationComponentSystem`，用于播放 `UnityAnimationNode` 对应的 `UnityEngine.Animation` 动画，并支持按 `animationComponentPath` 精确选择目标组件。
4. 精简 `SkelenAnimationComponent`，将眩晕监听与状态控制迁移到管理器，仅保留 Spine 动画绑定与播放职责。
5. 将单位创建、移动开始、移动停止、技能动画播放入口全部切换到 `AnimationManagerComponent`。
6. 将 `UnityAnimationNodeData.animationComponentPath` 贯通到运行时 `GameplayAbilitySpec -> SkillAnimationPlay -> AnimationManagerComponent`。

## 验证

- 已执行 AIBridge:
  - `focus --raw`
  - `editor stop --raw`
  - `compile unity --raw --timeout 120000`
- Unity 编译结果通过，无错误。
