# Client/Skill 按 ET 框架要求改造计划

## 问题陈述

当前 `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/` 同时承载了：

- `Data/` 下的节点数据、属性数据、标签数据
- `Runtime/` 下的技能执行、效果、条件、目标搜索、Battle 场景驱动、UI、特效控制器
- 多个 `MonoBehaviour`、`[EnableClass]` 普通类、直接 `GameObject` 引用与 `UnityEngine.Debug.Log*`

这与仓库当前的 ET 约束不一致，主要问题包括：

- `ModelView` 中混入大量 Unity 表现层与场景驱动逻辑
- 核心运行时对象仍以普通类承载，而不是 ET `Entity/Object` 体系
- 业务方法大量写在类内部，未迁移到 `System` 静态类
- 高变更玩法逻辑仍滞留在 `ModelView/Client/Skill/Runtime`，没有向 `Hotfix/HotfixView` 收敛

本次计划目标是：**将整个 `Client/Skill` 目录按仓库既有 ET 分层要求收口，明确数据层、运行时状态层、表现层与 Hotfix 逻辑层边界，并逐步完成迁移。**

## 背景和动机

当前 Skill 模块已经形成一套可运行的技能运行时，但其演进路径更接近“普通 Unity OO 技能框架”，而不是仓库主流的 ET 分层模式。继续在此结构上叠加功能，会持续放大以下问题：

1. 运行时逻辑、表现逻辑、编辑器序列化模型混杂，后续维护成本高。
2. 与分析器约束长期冲突，只能依赖 `[EnableClass]` 临时放行。
3. 技能、效果、条件、Cue 等高频玩法改动不在 Hotfix 层，后续热更与系统化扩展困难。
4. 直接持有 `GameObject` / `MonoBehaviour` / 实体引用，会让后续 Entity 生命周期治理越来越难。

## 范围

### 本次计划覆盖

- `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Data/**`
- `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/**`
- 与其直接对应的 `HotfixView/Client/Skill/**` 或 `Hotfix/Client/Skill/**` 新增/迁移逻辑

### 本次计划不直接覆盖

- Skill 编辑器工具本身的完整重构
- 与 Skill 模块弱相关的全局基础设施改造
- 非必要的底层库改造（优先在 `Assets/Scripts/Game/` 内解决）

## 现状摘要

### 1. Runtime 中混入表现层与场景驱动

典型文件：

- `Runtime/Battle/SkillUnit.cs`
- `Runtime/Core/GASHost.cs`
- `Runtime/UI/SkillBarUI.cs`
- `Runtime/UI/SkillSlotUI.cs`
- `Runtime/UI/UnitAttributeGUI.cs`
- `Runtime/Effect/ProjectileController.cs`
- `Runtime/Effect/PlacementController.cs`
- `Runtime/Cue/FloatingTextManager.cs`
- `Runtime/Cue/FloatingTextAnimator.cs`

这些文件包含 `MonoBehaviour`、场景对象引用、帧驱动与 UI 行为，不应长期停留在 `ModelView`。

### 2. 核心运行时对象仍是普通类

典型文件：

- `Runtime/Core/AbilitySystemComponent.cs`
- `Runtime/Ability/GameplayAbilitySpec.cs`
- `Runtime/Ability/AbilityContainer.cs`
- `Runtime/Effect/GameplayEffectSpec.cs`
- `Runtime/Effect/GameplayEffectContainer.cs`
- `Runtime/Core/SpecExecutionContext.cs`
- `Runtime/Core/SkillDataCenter.cs`

这些对象承载核心状态与流程，但仍主要依赖普通类 + `[EnableClass]`，未完全纳入 ET 生命周期与 `System` 组织方式。

### 3. Data 层存在“纯数据”和“运行时行为”混用

相对偏纯数据的文件：

- `Data/Base/SkillData.cs`
- `Data/Base/NodeData.cs`
- `Data/Effect/*NodeData.cs`
- `Data/Condition/*NodeData.cs`
- `Data/Cue/*NodeData.cs`

需要重点清理边界的文件：

- `Data/Attribute/ExecutionCalculation/ExecutionCalculation.cs`
- `Data/Attribute/AttributeModifier.cs`
- `Data/Tags/GameplayTagContainer.cs`

### 4. 约定一致性问题

- 多处直接使用 `UnityEngine.Debug.Log*`
- 运行时逻辑直接依赖 `GameObject` / `MonoBehaviour`
- 业务方法集中在类内部，未拆到 `XXXSystem`

## 改造原则

1. **纯编辑器/序列化数据继续留在 `Data/`，运行时实例状态与行为分离。**
2. **`ModelView` 优先只保留 ET 可接受的状态定义与必要桥接，不保留大块 MonoBehaviour 业务逻辑。**
3. **高频玩法逻辑优先迁往 `HotfixView` / `Hotfix`。**
4. **Entity/Object 类尽量不直接承载复杂业务方法，行为优先进入 `System` 静态类。**
5. **运行时不要继续扩大 Unity 表现层引用面；必要的 `GameObject` 引用通过桥接层收口。**
6. **日志、异步、命名、静态字段等实现细节遵循仓库既有约束。**

## 任务清单

- [x] 1. 盘点 `Client/Skill` 的完整引用面，确认 `Runtime` 与 `Data` 各类是否被 Scene、Prefab、Hotfix、Editor 工具直接依赖。
- [x] 2. 明确 `Data/` 中“纯序列化数据”和“运行时行为对象”的边界，整理保留清单与迁出清单。
- [ ] 3. 先迁出 `Runtime/UI/**`、`Runtime/Battle/**` 中明显属于表现层或场景驱动的 `MonoBehaviour` 逻辑，避免继续留在 `ModelView`。
- [ ] 4. 拆分 `Runtime/Effect/**`、`Runtime/Cue/**` 中的控制器类，把 `ProjectileController`、`PlacementController`、浮字管理等 View 驱动与技能运行时状态解耦。
- [ ] 5. 评估并重构 `AbilitySystemComponent`、`AbilityContainer`、`GameplayAbilitySpec`、`GameplayEffectSpec`、`GameplayEffectContainer`、`SpecExecutionContext`、`SkillDataCenter` 的 ET 承载方式。
- [ ] 6. 将核心运行时对象中的主要业务方法迁移到对应 `System` 静态类，减少 `ModelView` 普通类上的行为实现。
- [ ] 7. 将技能执行主链路（如 `SpecExecutor`、`SpecFactory`、`TaskSpec`、`ConditionSpec`、Target Provider 族）向 `HotfixView/Hotfix` 收口，并补齐必要桥接。
- [ ] 8. 收口运行时中的 Unity 对象引用方式，明确哪些属于 View 层临时引用，哪些需要转为 ET 可追踪引用。
- [ ] 9. 统一替换 Skill 模块中的 `UnityEngine.Debug.Log*`，改为项目约定日志接口。
- [ ] 10. 按模块分批验证改造后的编译、分析器与运行链路，优先覆盖技能授予、激活、效果执行、条件判断、Cue/投射物/放置物等典型流程。
- [ ] 11. 清理过渡期兼容代码，补齐最终目录落点与职责说明，确保 `Client/Skill` 分层清晰且后续可持续演进。

## 推荐实施顺序

### 阶段一：边界梳理

- 完成引用面盘点
- 确定 `Data` 保留项与迁移项
- 确定哪些 `Runtime` 文件属于表现层，哪些属于运行时状态层

### 阶段二：表现层剥离

- 优先迁移 Battle / UI / MonoBehaviour 控制器
- 保留必要兼容桥接，避免一次性打断现有 prefab/场景依赖

### 阶段三：核心状态 ET 化

- 处理 ASC、Ability、Effect、Context、DataCenter 等核心对象
- 确定它们在 ET 中的承载方式与生命周期关系

### 阶段四：行为迁移到 System / Hotfix

- 把主要技能执行逻辑转移到 `System`
- 把高频玩法规则迁到 `HotfixView/Hotfix`

### 阶段五：统一收尾与验证

- 清理日志、引用方式、兼容层
- 完成编译与关键玩法回归

## 技术考量

### 1. Data 与 Runtime 不应继续混层

若 `SkillData`、`NodeData` 等同时承担图编辑器序列化职责，则应保留其“编辑器数据模型”身份；运行时实例状态应单独建立，不直接把序列化模型强行改成 ET 运行时对象。

### 2. MonoBehaviour 迁移需要兼容顺序

`SkillUnit`、`GASHost`、`ProjectileController`、`PlacementController` 等如果已被 prefab 或场景直接依赖，应优先增加桥接层，再逐步收缩职责，避免一次性移除导致连锁回归。

### 3. 核心运行时对象要先收口，再决定最终 ET 形态

本次计划允许分阶段推进，不要求一步到位把所有类完全改成最终形态，但要先确保：

- 状态与表现分离
- 行为从类内方法向 `System` 收口
- 高变更逻辑迁往 Hotfix

### 4. 引用方式要提前统一

需要在改造早期就明确：

- 哪些对象允许暂时保留 Unity 引用
- 哪些运行时对象必须改为 ET 可管理引用
- 如何避免继续扩大直接实体/对象双向持有

## 风险

1. **Prefab / Scene 兼容风险**：若 Battle/UI/投射物类被大量挂载，迁移顺序不当会造成大面积回归。
2. **编辑器数据兼容风险**：若 `Data` 结构同时服务图编辑器和运行时，贸然修改可能破坏资源序列化。
3. **行为等价性风险**：技能、效果、Cue、投射物、放置物等链路长，拆层后容易出现生命周期顺序问题。
4. **过渡期混用风险**：若迁移过程中长期保留“双实现”，后续收尾成本会快速上升。

## 成功指标

1. `Client/Skill` 的 `Data`、运行时状态、表现层、Hotfix 行为边界清晰。
2. `ModelView/Client/Skill/Runtime` 中不再保留大块不必要的 `MonoBehaviour` 业务实现。
3. 核心技能运行时对象的行为大部分迁移到 `System` 静态类。
4. 高变更技能执行逻辑已迁到 `HotfixView/Hotfix` 的合适位置。
5. Skill 模块通过编译与分析器校验，且关键玩法链路回归通过。

## 验收标准

- `Client/Skill` 至少完成一轮明确的分层重组，不再依赖 `[EnableClass]` 兜底掩盖核心架构问题。
- 典型 Skill 运行流程可正常工作：授予、激活、条件判断、效果执行、Cue 表现、投射物/放置物链路。
- 改造后目录职责可被后续任务持续复用，不需要再回到“普通 Unity 运行时层”模式。
