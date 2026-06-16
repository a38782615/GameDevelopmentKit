# AttrCmp 整数视图改为截断

本次处理调整了 `AttrCmp` 的整数视图取整方式。

- `BaseValueInt` 从四舍五入改为直接截断小数部分。
- `CurrentValueInt` 从四舍五入改为直接截断小数部分。
- 底层 `float` 存储、事件派发、Clamp 与重算逻辑保持不变。
- 使用 AIBridge 执行 Unity 编译检查，编译通过且无错误。
