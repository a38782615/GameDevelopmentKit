# Skill/Runtime 类转换为 ET Entity 架构

## 问题陈述

当前 `Skill/Runtime` 下的核心类（AbilitySystemComponent、各种Container、Spec等）虽然在 `ET.Client` 命名空间下并标记了 `[EnableClass]`，但本质上是普通 C# 类：
- 通过构造函数手动创建，不走 ET 的 Entity 生命周期
- 用 `GASHost` MonoBehaviour 单例驱动 Tick，而非 ET 的 `IUpdate`
- 容器之间通过引用直接持有，而非 ET 的父子/组件关系
- 无法利用 ET 的对象池、序列化、热重载等基础设施

**目标**：将这些类转为 ET Entity/Component 体系，保持现有功能不变。

## 转换策略

### 核心原则

1. **Entity = 有独立生命周期的实例**（如每个技能实例、每个效果实例）
2. **Component = 挂载在Entity上的数据/功能模块**（如属性容器、标签容器）
3. **纯数据结构保持不变**（AbilityTagContainer struct、NodeData、SkillData 等）
4. **System 类放在 HotfixView** 中，Model 类放在 ModelView 中

### ET Entity 层级设计

```
Unit (ET已有)
└── AbilitySystemComponent [ComponentOf(Unit)]           ← 核心ASC
    ├── AttributeSetContainer                             ← 保持为普通类（纯数据容器）
    ├── GameplayTagContainer                              ← 保持为普通类（纯数据容器）
    ├── AbilityContainerComponent [ComponentOf(ASC)]      ← 技能容器
    │   └── GameplayAbilitySpec [ChildOf(Container)]      ← 每个技能实例是子Entity
    │       ├── SpecExecutionContext [ComponentOf(Spec)]   ← 执行上下文组件
    │       ├── TimeCueRuntimeComponent [ComponentOf(Spec)] ← 管理 List<TimeCueRuntime>
    │       └── TimeEffectRuntimeComponent [ComponentOf(Spec)]← 管理 List<TimeEffectRuntime>
    ├── GameplayEffectContainerComponent [ComponentOf(ASC)]← 效果容器
    │   └── GameplayEffectSpec [ChildOf(Container)]        ← 每个效果实例是子Entity
    ├── GameplayCueContainerComponent [ComponentOf(ASC)]   ← Cue容器（新增）
    │   └── GameplayCueSpec [ChildOf(Container)]           ← 每个Cue实例是子Entity
    └── ConditionSpec [ChildOf(ASC)]                       ← 条件实例（瞬时创建，执行后Dispose）

ConditionDispatcherComponent [Singleton]                   ← 条件分发器（参考AIDispatcherComponent）
└── 自动收集所有 [ConditionHandler] 标记的 AConditionHandler
    └── AttributeCompareConditionHandler                   ← 属性比较条件Handler
```

### 不转换为Entity的类（保持原样）

| 类 | 原因 |
|---|---|
| `AttributeSetContainer` | 纯数据容器，Dictionary管理属性，无独立生命周期 |
| `GameplayTagContainer` | 纯数据容器，带引用计数的标签集合 |
| `AbilityTagContainer` | struct，编译期标签配置 |
| `TimeCueRuntime` | 简单数据类，由 TimeCueRuntimeComponent 管理 |
| `TimeEffectRuntime` | 简单数据类，由 TimeEffectRuntimeComponent 管理 |
| `SkillDataCenter` | 单例，静态数据管理 |
| `SpecExecutor` | 静态工具类 |
| `SpecFactory` | 静态工厂类 |

## 转换任务清单

### 阶段一：核心 Entity 转换

- [x] 1. **AbilitySystemComponent → ET Component**
  - 改为继承 `Entity, IAwake<Unit>, IUpdate, IDestroy`
  - 添加 `[ComponentOf(typeof(Unit))]`
  - 移除构造函数，改用 `IAwake` 初始化
  - 移除 `GASHost` 注册/注销，改用 `IUpdate` 驱动 Tick
  - `Owner` 从 `GameObject` 改为通过 `Unit` 获取
  - `Attributes`、`OwnedTags` 保持为普通类成员
  - 事件（OnAbilityActivated 等）保持 C# event

- [x] 2. **AbilityContainer → AbilityContainerComponent**
  - 改为继承 `Entity, IAwake, IUpdate, IDestroy`
  - 添加 `[ComponentOf(typeof(AbilitySystemComponent))]`
  - `_grantedAbilities` 和 `_activeAbilities` 改为管理子Entity
  - 移除构造函数中的 `_owner` 参数，通过 `this.Parent` 获取 ASC

- [x] 3. **GameplayAbilitySpec → ET Entity**
  - 改为继承 `Entity, IAwake<SkillData, AbilitySystemComponent>, IUpdate, IDestroy`
  - 添加 `[ChildOf(typeof(AbilityContainerComponent))]`
  - `SpecId` 改用 `Entity.Id`
  - `Owner` 通过父级链获取 ASC
  - `_timeEffects` 和 `_timeCues` 改为子Component
  - `_runningEffects` 保持为 List（引用其他Entity）

- [x] 4. **GameplayEffectContainer → GameplayEffectContainerComponent**
  - 改为继承 `Entity, IAwake, IUpdate, IDestroy`
  - 添加 `[ComponentOf(typeof(AbilitySystemComponent))]`
  - `_activeEffects` 改为管理子Entity
  - 通过 `this.Parent` 获取 ASC

- [x] 5. **GameplayEffectSpec → ET Entity**
  - 改为继承 `Entity, IAwake<string, string, SpecExecutionContext>, IUpdate, IDestroy`
  - 添加 `[ChildOf(typeof(GameplayEffectContainerComponent))]`
  - `SpecId` 改用 `Entity.Id`
  - 子类（DamageEffectSpec、BuffEffectSpec 等）同步修改

### 阶段二：执行上下文与Cue转换

- [x] 6. **SpecExecutionContext → ET Component**
  - 改为继承 `Entity, IAwake, IDestroy`
  - 添加 `[ComponentOf(typeof(GameplayAbilitySpec))]`
  - 目标管理方法（SetTargets/AddTarget/ClearTargets）保留
  - 位置获取方法（GetPosition/GetSourceObject）保留
  - `Caster`、`MainTarget` 等从直接引用改为存 Entity Id，通过 Id 获取 ASC Entity
  - `CreateWithParentInput` 改为创建新的 Component 实例
  - `ProjectileObject`、`PlacementObject` 保持 GameObject 引用（View层对象）

- [x] 7. **GameplayCueSpec → ET Entity**
  - 改为继承 `Entity, IAwake<string, string, SpecExecutionContext>, IUpdate, IDestroy`
  - 添加 `[ChildOf(typeof(GameplayCueContainerComponent))]`（新增Cue容器）
  - `SpecId` 改用 `Entity.Id`
  - 抽象方法 `PlayCue`/`StopCue` 保留（子类重写）
  - `Execute`/`Tick`/`Cancel`/`Stop` 生命周期方法保留
  - 子类（ParticleCueSpec、SoundCueSpec、FloatingTextCueSpec）同步修改

- [x] 8. **新增 GameplayCueContainerComponent**
  - 继承 `Entity, IAwake, IUpdate, IDestroy`
  - 添加 `[ComponentOf(typeof(AbilitySystemComponent))]`
  - 管理所有运行中的 CueSpec 子Entity
  - 替代原来分散在 Effect/TimeCue 中的 Cue 管理逻辑

- [x] 9. **ConditionSpec → Dispatcher + Handler 模式（参考 AIDispatcherComponent）**
  - 新增 `ConditionHandlerAttribute : BaseAttribute` 标记条件Handler类
  - 新增 `AConditionHandler : HandlerObject` 抽象基类
    - `abstract bool Evaluate(ConditionSpec conditionSpec, AbilitySystemComponent target)` 
  - 新增 `ConditionDispatcherComponent : Singleton, ISingletonAwake`
    - Awake 时通过 `CodeTypes.Instance.GetTypes(typeof(ConditionHandlerAttribute))` 自动收集所有 Handler
    - 提供 `Get(NodeType)` 按条件类型查找 Handler
  - **ConditionSpec → ET Entity**
    - 改为继承 `Entity, IAwake<string, string, SpecExecutionContext>, IDestroy`
    - 添加 `[ChildOf(typeof(AbilitySystemComponent))]`（瞬时创建，执行后 Dispose）
    - `Execute()` 改为通过 Dispatcher 查找 Handler 执行 Evaluate
  - **AttributeCompareConditionSpec → AttributeCompareConditionHandler**
    - 改为继承 `AConditionHandler`，标记 `[ConditionHandler]`
    - 原 `Evaluate` 逻辑迁移到 Handler 中
    - 不再继承 ConditionSpec，改为独立的 Handler 类

### 阶段三：辅助组件创建

- [x] 10. **新增 TimeCueRuntimeComponent**
  - 继承 `Entity, IAwake, IDestroy`
  - 添加 `[ComponentOf(typeof(GameplayAbilitySpec))]`
  - 内部持有 `List<TimeCueRuntime>` 管理所有时间Cue数据
  - TimeCueRuntime 保持为普通数据类不变
  - 提供 Reset/Check 等方法，原 GameplayAbilitySpec 中的 `_timeCues` 相关逻辑迁移到此组件
  - `TriggeredCueSpecs` 改为存 Entity Id 列表（引用 CueSpec Entity）

- [x] 11. **新增 TimeEffectRuntimeComponent**
  - 继承 `Entity, IAwake, IDestroy`
  - 添加 `[ComponentOf(typeof(GameplayAbilitySpec))]`
  - 内部持有 `List<TimeEffectRuntime>` 管理所有时间效果数据
  - TimeEffectRuntime 保持为普通数据类不变
  - 提供 Reset/Check 等方法，原 GameplayAbilitySpec 中的 `_timeEffects` 相关逻辑迁移到此组件

### 阶段四：System 类创建（HotfixView）

- [x] 12. **AbilitySystemComponentSystem** - ASC 的 Awake/Update/Destroy 逻辑
- [x] 13. **AbilityContainerComponentSystem** - 技能容器的生命周期和管理逻辑
- [x] 14. **GameplayAbilitySpecSystem** - 技能实例的激活/Tick/结束逻辑
- [x] 15. **GameplayEffectContainerComponentSystem** - 效果容器的管理逻辑
- [x] 16. **GameplayEffectSpecSystem** - 效果实例的执行/Tick/移除逻辑
- [x] 17. **SpecExecutionContextSystem** - 上下文的创建/销毁逻辑
- [x] 18. **GameplayCueSpecSystem** - Cue的Execute/Tick/Cancel逻辑
- [x] 19. **GameplayCueContainerComponentSystem** - Cue容器的管理和Tick逻辑
- [x] 20. **ConditionSpecSystem** - 条件Entity的Awake/Destroy逻辑
- [x] 21. **ConditionDispatcherComponentSystem** - Dispatcher的Awake收集Handler逻辑
- [x] 22. **TimeCueRuntimeComponentSystem** - 时间Cue的触发逻辑
- [x] 23. **TimeEffectRuntimeComponentSystem** - 时间效果的触发逻辑

### 阶段五：适配与清理

- [x] 24. **移除 GASHost** - 标记为 Obsolete，保留空壳避免编译错误
- [x] 25. **修改 SkillUnit** - 改为通过 ET 的 Unit 创建 ASC Component
- [x] 26. **修改 SpecExecutor** - 适配新的 Entity 创建方式（AddChild 替代 new），条件节点改为通过 ConditionDispatcher 执行
- [x] 27. **修改 SpecFactory** - 移除 `CreateConditionSpec`，Effect/Cue 改为通过容器 AddChild 创建 Entity
- [x] 28. **修改 GameplayCueManager** - 更新注释，保持 View 层资源管理不变
- [x] 29. **修改 Battle 相关类** - Player、Monster 适配新 ASC 访问方式

## 技术考量

### Entity 创建方式变更

```csharp
// 旧方式
var spec = new GameplayAbilitySpec(graphData, owner);

// 新方式
var spec = abilityContainer.AddChild<GameplayAbilitySpec, SkillData, AbilitySystemComponent>(graphData, asc);
```

### Tick 驱动变更

```csharp
// 旧方式：GASHost.Update() → asc.Tick()
// 新方式：ET IUpdate 自动驱动

[EntitySystemOf(typeof(AbilitySystemComponent))]
public static partial class AbilitySystemComponentSystem
{
    [EntitySystem]
    private static void Update(this AbilitySystemComponent self)
    {
        float dt = TimeInfo.Instance.DeltaTime / 1000f;
        self.GetComponent<AbilityContainerComponent>()?.Tick(dt);
        self.GetComponent<GameplayEffectContainerComponent>()?.Tick(dt);
    }
}
```

### 子类继承问题

`GameplayEffectSpec` 有多个子类（DamageEffectSpec、BuffEffectSpec 等）。ET Entity 不推荐继承，但可以：
- **方案A**：保持继承，子类也是 Entity（ET 支持 Entity 继承）
- **方案B**：改为组合模式，用不同 Component 区分效果类型
- **推荐方案A**：改动最小，现有子类重写的虚方法逻辑保持不变

### 风险点

1. **事件系统**：现有 C# event 在 Entity Dispose 时需要手动清理，否则内存泄漏
2. **循环引用**：ASC ↔ EffectSpec 之间有双向引用，需确保 Dispose 顺序正确
3. **SpecExecutionContext 生命周期**：原来是临时对象随用随创建，转为 Component 后需注意：
   - `CreateWithParentInput` 会创建新上下文，需要决定挂载到哪个 Entity 上
   - 持有的 `Caster`/`MainTarget` 等 ASC 引用改为 Entity Id 后，需处理 Entity 已 Dispose 的情况
   - 多个 EffectSpec 可能共享同一个 Context，转 Component 后需考虑是共享还是各自持有
4. **GameplayCueSpec 抽象继承**：ET Entity 继承链 + 抽象方法，子类（ParticleCueSpec 等）需同步改造
5. **Cue 生命周期管理变更**：原来 Cue 由 Effect 的 `_triggeredCueSpecs` 列表管理，转 Entity 后改为通过 CueContainer 统一管理，Effect 只存 Cue 的 Entity Id
6. **性能**：Entity 创建/销毁比 new 对象开销大，频繁创建的瞬时效果和瞬时Cue需关注
7. **ConditionSpec Dispatcher 模式变更**：原来 ConditionSpec 通过继承实现多态（AttributeCompareConditionSpec 继承 ConditionSpec），改为 Dispatcher + Handler 后，SpecExecutor 和 SpecFactory 中条件相关的创建和执行逻辑需全部适配

## 成功指标

1. 所有 Skill/Runtime 核心类（ASC、Container、Spec、Context、CueSpec、ConditionSpec）纳入 ET Entity 生命周期管理
2. 移除 GASHost MonoBehaviour，完全由 ET Update 驱动
3. SpecExecutionContext 作为 Component 挂载在 AbilitySpec 上，支持上下文传递和生命周期管理
4. GameplayCueSpec 及子类作为 Entity 由 CueContainer 统一管理
5. ConditionSpec 采用 Dispatcher + Handler 模式，参考 AIDispatcherComponent，自动收集条件Handler
6. 现有技能系统功能（激活、效果、冷却、Buff、Cue、条件判断）全部正常工作
7. SkillUnit/Player/Monster 等使用方代码适配完成

## 依赖

- ET 框架的 Entity 基类、ComponentOf/ChildOf 特性
- ET 的 EntitySystemOf、IAwake、IUpdate、IDestroy 接口
- 现有 Unit Entity（ASC 将挂载在 Unit 上）

## 文件位置规划

| 类型 | 路径 |
|---|---|
| Model（Entity定义） | `ET/Code/ModelView/Client/Skill/Runtime/Core/` |
| Hotfix（System逻辑） | `ET/Code/HotfixView/Client/Skill/Runtime/Core/` |
| 数据类（不变） | `ET/Code/ModelView/Client/Skill/Data/` |
