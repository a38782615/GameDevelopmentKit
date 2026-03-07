# ProjectileController 按 ET 模式拆分计划

## 问题陈述

当前投射物逻辑集中在 `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileController.cs` 这个 `MonoBehaviour` 中，包含：

- 初始化参数保存
- `Update` 驱动飞行
- 碰撞检测与目标过滤
- 命中 / 到达 / 反弹 / 销毁事件
- Transform 位置与朝向同步

同时，`ProjectileEffectSpec` 直接持有 `ProjectileController`：

- `ProjectileEffectSpec._projectileController`
- `ProjectileEffectSpecHandler` 直接 `GetComponent<ProjectileController>() / AddComponent<ProjectileController>()`

这与当前仓库已经成型的 ET 拆分方式不一致，也与 `Share/Analyzer` 中的 Entity 约束存在冲突风险，尤其是：

- Entity 内不应直接持有 Entity/运行时对象式业务引用
- ET 运行时逻辑应优先落在 `ModelView + HotfixView System` 结构中
- entity 引用应优先使用 `EntityRef<>`

## 背景和动机

当前 Skill Runtime 其余核心结构已经基本按 ET 方式拆分：

- 数据 / 运行时状态定义放在 `ModelView`
- 逻辑放在 `HotfixView` 的 `System` 或 `Handler`
- `AbilitySystemComponent`、`GameplayEffectSpec`、`GameplayCueSpec` 等都已转入 ET 生命周期

但 `ProjectileController` 仍保留传统 Unity `MonoBehaviour Update` 驱动方式，导致投射物链路出现“半 ET、半 MonoBehaviour”的混合实现。继续在这个模式上叠加功能，会带来：

- 生命周期分散，调试入口不统一
- `ProjectileEffectSpec` 与 View 层控制器耦合过深
- 后续要把 target/source/hit 缓存统一成 `EntityRef<>` 时改动面继续扩大

本次计划目标是：**将 ProjectileController 的业务状态与行为迁移到 ET 运行时结构中，并仅保留必要的 Unity View 层对象引用。**

## 现状分析

### 当前涉及文件

- `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileController.cs`
- `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileEffectSpec.cs`
- `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileInitData.cs`
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Effect/ProjectileEffectSpecHandler.cs`

### 当前问题点

1. `ProjectileController` 同时承担“数据 + 行为 + Unity 生命周期 + 事件通知”。
2. `ProjectileEffectSpec` 直接持有 `ProjectileController`，不符合当前 ET 化后的引用收敛方向。
3. `ProjectileInitData` 中的 `TargetUnit`、`SourceASC` 仍是直接实体引用，未统一到 `EntityRef<>`。
4. 运行时命中缓存 `_hitTargets` 是 `HashSet<AbilitySystemComponent>`，如果迁移到 Entity，将继续触发引用方式不统一的问题。
5. `ProjectileEffectSpecHandler` 目前直接与 MonoBehaviour 交互，ET 与 View 的边界不清晰。

## 目标

### 主要目标

- 将投射物核心运行时状态迁移为 ET 组件/实体数据。
- 将飞行、碰撞、反弹、命中、销毁等逻辑迁移到 `HotfixView` System。
- 让 `ProjectileEffectSpec` 不再直接依赖 `ProjectileController`，而是依赖 ET 运行时对象。
- 将投射物链路中的 entity 引用统一收敛到 `EntityRef<>` 或可追踪的 Id。

### 非目标

- 不在本次计划中重构整个 Placement / Cue / Target 搜索体系。
- 不主动扩散到所有同类 Controller，除非本次拆分必须抽取公共能力。
- 不改动技能编辑器节点表现层配置结构，除非为兼容运行时必须做最小调整。

## 拆分方案

### 方案摘要

将 `ProjectileController` 拆分为：

1. **ET 运行时组件**：保存投射物状态、初始化参数、命中缓存、运行标记。
2. **HotfixView System**：负责 Tick、位移、碰撞检测、目标过滤、反弹、销毁流程。
3. **Unity View 引用**：`ProjectileEffectSpec` 仅继续持有投射物 `GameObject`，必要时保留一个极薄的 MonoBehaviour 桥接壳用于 Gizmos / 调试，但不再承载业务逻辑。

### 推荐结构

#### ModelView

- 新增 `ProjectileRuntimeComponent`（名称可在实现时再最终确认）
  - 建议路径：`ModelView/Client/Skill/Runtime/Effect/ProjectileRuntimeComponent.cs`
  - 建议声明：`[ComponentOf(typeof(ProjectileEffectSpec))]`
  - 仅保存运行时数据，不在类内写复杂业务方法

- 修改 `ProjectileEffectSpec`
  - 移除 `ProjectileController _projectileController`
  - 新增 `EntityRef<ProjectileRuntimeComponent>` 或通过 `GetComponent<ProjectileRuntimeComponent>()` 获取运行时组件
  - 保留 `GameObject _projectileObject` 作为 View 层对象引用

- 修改 `ProjectileInitData`
  - `TargetUnit` 改为 `EntityRef<AbilitySystemComponent>`
  - `SourceASC` 改为 `EntityRef<AbilitySystemComponent>`
  - 视需要将命中链路里会长期保存的实体引用同步改为 `EntityRef<>`

#### HotfixView

- 新增 `ProjectileRuntimeComponentSystem`
  - 初始化运行时状态
  - Tick 投射物移动
  - 检测碰撞并筛选 ASC
  - 执行命中 / 反弹 / 到达目标 / 超距销毁
  - 同步 `GameObject.transform`

- 修改 `ProjectileEffectSpecHandler`
  - 负责创建投射物 `GameObject`
  - 通过 `ProjectileEffectSpec.AddComponent<ProjectileRuntimeComponent>()` 初始化 ET 运行时组件
  - 取消直接订阅 `ProjectileController` C# 事件的方式
  - 改为在 ET 运行时组件 / System 中回调或直接驱动 effect 端口执行

### 关于 `ProjectileController` 本体的处理

优先级建议如下：

- **首选**：删除其业务职责，仅保留极薄 View 壳（例如 Gizmos、调试辅助）。
- **次选**：如果 prefab/场景上没有任何实际依赖，则直接移除该脚本的运行时使用。
- **避免**：保留一个仍带 `Update()` 的半业务控制器，否则这次 ET 拆分收益会大幅下降。

## 任务清单

- [x] 1. 盘点 `ProjectileController` 的职责边界，明确哪些属于 ET 运行时逻辑、哪些属于纯 View 表现。
- [x] 2. 设计并新增 `ProjectileRuntimeComponent`（或最终确认的等价命名），只保留数据字段与最小必要状态。
- [x] 3. 将 `ProjectileEffectSpec` 中对 `ProjectileController` 的直接持有改为 ET 组件引用方式。
- [x] 4. 调整 `ProjectileInitData`，把 `TargetUnit`、`SourceASC` 等实体引用改为 `EntityRef<>`。
- [x] 5. 梳理命中缓存、反弹目标、当前目标等运行时集合，避免继续保存直接实体对象引用。
- [x] 6. 在 `HotfixView` 新增 `ProjectileRuntimeComponentSystem`，迁移初始化、移动、旋转、碰撞、到达、销毁逻辑。
- [x] 7. 修改 `ProjectileEffectSpecHandler`，改为通过 ET 运行时组件驱动投射物，而不是直接操作 `ProjectileController` 事件。
- [x] 8. 统一投射物命中、反弹、到达目标、销毁后的 effect 端口触发方式，确保行为与现状一致。
- [x] 9. 评估 `ProjectileController` 是否需要保留为极薄桥接壳；若不需要，则移除其运行时职责。
- [x] 10. 酌情修改被投射物链路直接引用到的类，优先包括 `ProjectileEffectSpec`、`ProjectileInitData`、`ProjectileEffectSpecHandler`，必要时抽取少量公共辅助逻辑。
- [ ] 11. 完成后执行编译 / 分析器校验，确认没有新增 ET 分析器约束问题。
- [ ] 12. 验证典型场景：直线投射、曲线投射、单位追踪、穿透、反弹、到达目标、取消 Effect 销毁。

> 当前进度说明：已完成主体 ET 拆分与本次改动相关的编译问题收敛；整体构建仍被仓库内既有的 `ConditionSpecSystem` 错误及多处 `ET0013` 静态类循环依赖阻塞，因此第 11、12 项暂未完成。

## 技术考量

### 1. Entity 引用方式

本次拆分需要优先遵守当前分析器约束：

- Entity 中不要继续保存 `AbilitySystemComponent` 这类直接实体字段
- 优先使用 `EntityRef<AbilitySystemComponent>`
- 集合场景优先使用 `List<EntityRef<T>>` / `HashSet<long>` / 其他不会扩大直接实体持有范围的方式

### 2. 事件模型是否保留

当前 `ProjectileController` 用 C# event 通知 Handler：

- `OnHit`
- `OnReachTarget`
- `OnBounce`
- `OnDestroy`

拆到 ET 后，建议不要简单把同一套 event 原样搬进运行时实体，而是评估以下两种方式：

- **方案 A：System 直接驱动 `ProjectileEffectSpec` 的后续端口逻辑**
- **方案 B：保留最小回调机制，但事件只作为内部桥接，不再由 MonoBehaviour 持有主业务流程**

推荐优先 **方案 A**，这样边界更清晰。

### 3. Unity View 仍可保留

即便逻辑 ET 化，投射物依然需要 `GameObject` 承载：

- prefab 实例化
- transform 同步
- Gizmos / 调试可视化

因此本次并不是“完全去掉 Unity 对象”，而是“去掉 Unity `MonoBehaviour Update` 对业务流程的主导地位”。

### 4. 与 PlacementController 的关系

`PlacementController` 目前仍是类似模式，但本次计划不主动一并重构。仅在出现以下情况时才顺带调整：

- 需要抽取公共的 `Collider -> ASC` 获取逻辑
- 需要统一 `SkillUnit.ASC` 的访问方式
- 当前拆分如果不顺手修复会导致编译或分析器错误

## 成功指标

1. `ProjectileController` 不再承担主业务逻辑的 `Update` 驱动。
2. 投射物主状态与行为进入 ET `ModelView + HotfixView System` 结构。
3. `ProjectileEffectSpec` 不再直接持有 `ProjectileController`。
4. 投射物链路中的实体引用统一为 `EntityRef<>` 或等价的可追踪引用。
5. 现有行为保持一致：
   - 直线 / 曲线飞行
   - 单位追踪
   - 点目标 / FlyOver
   - 穿透
   - 反弹
   - 命中 / 到达 / 销毁后的节点触发
6. 新实现能够通过现有编译与分析器约束。

## 依赖与风险

### 依赖

- 当前 Skill Runtime 已完成的 ET 化基础设施
- `ProjectileEffectSpec` / `GameplayEffectSpec` 的现有生命周期
- `AbilitySystemComponent`、`SkillUnit`、`SpecExecutionContext` 的现有引用链路

### 风险

1. **行为等价性风险**：曲线飞行、追踪飞行、穿透和反弹混合场景较多，迁移时容易漏边界条件。
2. **引用切换风险**：`AbilitySystemComponent -> EntityRef<>` 后，空引用判断与 Owner 访问方式需要逐处调整。
3. **销毁顺序风险**：Effect 取消、投射物自毁、节点回调三者顺序若处理不当，可能出现重复销毁或重复触发。
4. **Prefab 兼容风险**：若某些预制体显式挂了 `ProjectileController`，需要决定是保留桥接壳还是统一迁移。

## 文件落点建议

| 类型 | 路径建议 |
|---|---|
| 运行时数据 | `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileRuntimeComponent.cs` |
| Effect 引用调整 | `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileEffectSpec.cs` |
| 初始化数据调整 | `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Effect/ProjectileInitData.cs` |
| 热更逻辑 | `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Effect/ProjectileRuntimeComponentSystem.cs` |
| Handler 适配 | `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Effect/ProjectileEffectSpecHandler.cs` |

## 后续执行建议

1. 运行 `/workflows-work plans/projectile-controller-et-split.md`
2. 实现完成后运行 `/workflows-review`
3. 最后运行 `/workflows-compound` 记录这次 ET 拆分的约束与经验
