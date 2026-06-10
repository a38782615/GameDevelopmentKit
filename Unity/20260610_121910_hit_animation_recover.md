# 受击动画恢复修复总结

## 问题

单位受击后播放 `BeAttack` 非循环动画，动画驱动没有在非循环动画结束后恢复到循环动作，导致角色停在受击最后一帧，看起来像动画暂停。

## 修改

- `AnimationManagerComponent` 增加当前动画名和动画版本号，用于判断延迟恢复期间是否切换过其他动画。
- `AnimationManagerComponentSystem.PlayAnimation` 改为返回播放结果，并在播放成功后更新当前动画状态。
- Spine 和 Unity Legacy Animation 驱动改为返回播放结果；Spine 非循环同名动画允许重新播放，支持连续受击。
- 受击表现改为异步流程：播放 `BeAttack`，等待动画长度，若期间没有切到其他动画且单位仍存活，则恢复到移动动画或待机动画。

## 验证

- 已使用 AIBridge 停止 Unity Play Mode。
- 已执行 `AIBridgeCLI.exe compile unity --raw --timeout 120000`，编译通过，错误 0，警告 0。
