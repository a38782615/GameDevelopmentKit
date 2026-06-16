# AttrCmp 增加 int 视图属性

本次处理为 `AttrCmp` 增加了一套整数视图属性。

- 新增 `BaseValueInt`，基于 `BaseValue` 四舍五入得到整数值。
- 新增 `CurrentValueInt`，基于 `CurrentValue` 四舍五入得到整数值。
- 保持底层 `float + NumericComponent` 存储与事件链路不变，只补充读取视图。
- 同时将 `GameAIComponentSystem.Utility` 中攻击间隔读取切换为 `CurrentValueInt`，消除现有 `float -> int` 编译错误。
- 使用 AIBridge 执行 Unity 编译检查，编译通过且无错误。
