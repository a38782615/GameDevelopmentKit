# EntityBody Shape 枚举化

## 修改

- 将 `EntityBody` 中的 `CircleShape`、`RectangleShape` 从 `const int` 改为 `EntityBody.ShapeType` 枚举成员。
- 将 `EntityBody.Shape` 字段从 `int` 改为 `EntityBody.ShapeType`。
- `EntityBodySystem` 读取 `DRUnitConfig.Shape` 时显式转换为 `EntityBody.ShapeType`。
- 更新 `BodyCheckComponentSystem` 和 `MovementAgentRuntime` 中的形状判断逻辑。

## 验证

- 已通过 AIBridge 停止 Play Mode。
- 已执行 Unity 编译检查：`errorCount=0`，`warningCount=0`。
- 已检查本次涉及代码文件，未发现新增中文注释或中文字符串编码异常。
