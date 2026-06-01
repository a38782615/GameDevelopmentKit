## 本次任务

修正 `UnityAnimationNode` 的预览语义，确保它预览的是预制体资源上的 `Animation` 节点，而不是场景对象或错误的采样根。

## 主要改动

- `UnityAnimationNode` 现在只接受 prefab asset；如果拖入场景对象，会自动清空并给出警告。
- `UnityAnimationPreviewRenderer` 采样动画时，改为对“选中的 Animation 组件所在对象”执行 `SampleAnimation`，不再固定对预制体实例根节点采样。
- 当当前引用不是 prefab asset 时，节点会自动清空预览和绑定选择，避免错误状态残留。

## 验证

- 已执行 `AIBridgeCLI focus --raw`
- 已执行 `AIBridgeCLI editor stop --raw`
- 已执行 `AIBridgeCLI compile unity --raw --timeout 120000`
- 编译结果：成功，`errorCount = 0`

## 说明

- 未执行 git 提交。
