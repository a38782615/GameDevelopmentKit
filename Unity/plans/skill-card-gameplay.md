# 技能卡牌化玩法改造计划

## 目标

将当前“单位直接持有主动/被动技能并在运行时直接授予”的模式，改为“技能抽象成卡牌，并叠加遗物系统”的玩法模式。

本次计划先按以下规则落地：

1. 技能牌分为：抽牌区、出牌区、能力区、弃牌区、销毁区。
2. 初始抽牌数量、移动扣点速率、基础轮转时长、出牌区上限等各种数值统一走配置，不在运行时代码中写死。
3. 释放技能时，扣除对应技能当前有效消耗值。
4. 单位移动时开始计时，按配置定义的速率扣除技能点；停止移动则停止计时。
5. 轮转时按配置规则重置技能点，并将出牌区整体丢弃到弃牌区，再按配置数量从抽牌区补牌；该轮转时长后续允许被技能效果动态修改。
6. 抽牌区为空时，将弃牌区洗回抽牌区。
7. 出牌区最多保留配置规定数量的技能牌，超出的直接进入弃牌区。

## 当前系统现状

当前仓库里与该玩法直接相关的链路如下：

- 技能授予：
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Battle/SkillUnitSystem.cs`
  - 当前会从 `DRHero.ActiveSkill` / `DRMonster.ActiveSkill` 直接授予 `GameplayAbilitySpec`
  - 被动技能当前来自 `PassiveSkill`，并在初始化后尝试自动激活
- 技能运行时：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Core/AbilitySystemComponent.cs`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Core/AbilitySystemComponentSystem.cs`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Ability/GameplayAbilitySpecSystem.cs`
- 技能 UI：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UISkill/UIFormSkillComponent.cs`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UISkill/UIFormSkillComponentSystem.cs`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UISkill/SkillCellComponentSystem.cs`
  - 当前 UI 是直接同步 `GrantedAbilities`
- 移动输入与移动链路：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UIInput/FightInputCallbacks.cs`
  - `Assets/Scripts/Game/ET/Code/Hotfix/Client/GenAtom/Main/Move/MoveHelper.cs`
  - `Assets/Scripts/Game/ET/Code/Hotfix/Share/Module/Move/Move2DComponentSystem.cs`
  - 当前没有“移动中持续扣技能点”的玩法层状态管理

## 设计约定

为避免一上来就推翻现有技能运行时，本次先按“技能运行时不变，外面增加卡牌层”设计：

- `GameplayAbilitySpec` 继续作为真实可施放技能实例。
- 新增“技能卡牌运行时组件”负责抽牌区、出牌区、能力区、弃牌区、销毁区的管理。
- 新增“遗物运行时组件”负责战斗中遗物的持有、触发和数值修正。
- UI 不再直接展示 `GrantedAbilities`，而是展示“出牌区中的卡牌”。
- 主动技能和被动技能都需要卡牌化，不再保留“被动技能初始化后直接自动激活”的旧默认逻辑。
- 第一版不改技能图执行链路，不手改生成配置代码，优先做玩法层包裹。

### 关键术语约定

- 抽牌区：当前待抽的技能卡集合。
- 出牌区：当前可操作的技能卡集合。实现时建议等价于“手牌区”。
- 能力区：被动技能卡打出后驻留的区域。驻留中的被动技能卡按规则周期触发。
- 弃牌区：轮转结束或溢出后暂存的技能卡集合。
- 销毁区：被永久移出的技能卡集合，第一版先只保留结构，不强行接玩法。
- 技能点：归属于人物属性系统，当前明确复用现有 `MP` 属性；卡牌消耗的是角色当前 `MP`，而不是卡牌私有资源。
- 技能消耗值来源于配表；战斗运行时允许被技能效果、Buff 或其他玩法动态修改，但应通过“运行时覆盖值”生效，不直接手改 Luban 生成的静态配置对象。
- 抽牌数量、移动扣点、轮转时长、出牌区上限等所有玩法数值统一来源于配置；代码只负责读取和结算，不持有硬编码常量。

## 分阶段任务

- [ ] 1. 新增卡牌运行时领域模型，明确 `SkillCardRuntime`、`SkillCardDeckComponent`、牌区枚举、抽牌/弃牌/洗牌/销毁接口。
- [ ] 2. 明确卡牌与技能实例的绑定关系。
  - 建议每张卡牌持有 `GameplayAbilitySpec` 的弱引用或实体 Id。
  - 同一技能允许在牌堆中出现多份副本，因此卡牌实例需要独立 `CardInstanceId`，不能仅以 `SkillId` 作为 UI 和牌区主键。
- [ ] 3. 调整 `SkillUnitSystem` 的技能初始化流程。
  - 同时覆盖主动技能与被动技能的初始化。
  - 保留 `GrantAbility` 创建真实技能实例。
  - 不再把“授予的全部技能”直接视为可出牌技能或自动激活技能。
  - 初始化时将主动技能与被动技能一并映射为抽牌区卡牌池。
- [ ] 4. 新增卡牌轮转组件的生命周期与计时驱动。
  - 建议挂在 `SkillUnit` 或 `AbilitySystemComponent` 下。
  - 负责首抽、基础轮转、牌区上限控制。
  - 轮转时长不能写死常量，需支持被技能效果、Buff 或角色属性动态修改。
- [ ] 5. 实现首轮抽牌与补牌规则。
  - 初始化后从抽牌区按配置数量抽牌到出牌区。
  - 抽牌区为空时，将弃牌区洗回抽牌区后继续抽。
  - 出牌区超过配置上限时，超出部分进入弃牌区。
- [ ] 6. 实现技能点数据模型。
  - 技能点改为角色属性，当前明确直接复用现有 `MP` 读写链路。
  - 需要新增或扩展配表来源，至少包含：初始 `MP` / 最大 `MP`、抽牌数量、移动扣点速率、基础轮转时长、出牌区上限。
  - 技能释放消耗值来源于技能配表，并在运行时读取“当前有效消耗值”。
  - 当前有效消耗值允许被技能效果动态修改，但应存放在运行时状态或覆盖表中，不直接修改 `DRSkill` 之类的静态配置实例。
  - 释放技能按当前有效消耗值扣角色当前 `MP`。
  - 轮转时按规则将角色 `MP` 直接恢复到 `MaxMp`，而不是逐卡重置。
- [ ] 7. 让技能释放改为“从卡牌发起”。
  - UI 点击不再直接拿 `GameplayAbilitySpec` 施放。
  - 先校验卡牌是否在出牌区、角色当前 `MP` 是否足够，再调用 `asc.TryActivateAbility(spec)`。
  - 技能施放成功后再扣角色 `MP`，失败不扣点。
  - 施放前的消耗校验与施放后的扣费必须读取同一份运行时消耗值，避免展示值、校验值、实际扣费值不一致。
- [ ] 8. 补移动扣点链路。
  - 从输入层或移动组件层识别“开始移动 / 停止移动”。
  - 只在移动中累计时间。
  - 按配置定义的时间片和扣点速率结算，并处理小数累计残留。
- [ ] 9. 定义“移动扣点”对哪些卡生效。
  - 当前确认移动扣的是角色 `MP`，不是单卡技能点。
  - 卡牌系统只负责限制“哪些技能当前可出牌/可操作”，资源消耗走角色公共资源池。
- [ ] 10. 改造 `UIFormSkillComponentSystem`。
  - 列表数据源从 `GrantedAbilities` 改为 `出牌区卡牌列表`。
  - 展示牌区、能力区、技能类型（主动/被动）、剩余技能点、可施放状态、溢出与洗牌后的刷新结果。
- [ ] 11. 改造 `SkillCellComponentSystem`。
  - `Bind` 对象从 `GameplayAbilitySpec` 扩展为“技能卡视图数据”。
  - 状态文案增加技能点显示与主动/被动标识。
  - 点击释放改为走卡牌组件；被动技能卡打出后进入能力区。
- [ ] 12. 为卡牌轮转补诊断日志。
  - 抽牌、弃牌、洗牌、销毁、轮转重置、移动扣点、施法扣点统一记录到 `SkillDiagFileLogger.Log(...)`。
- [ ] 13. 新增遗物系统的配置与运行时骨架。
  - 新增遗物配置表，承载遗物基础定义、触发条件、数值修正与描述。
  - 新增遗物运行时组件，负责角色当前持有遗物、遗物触发和数值修正入口。
  - 明确遗物对哪些目标生效：单卡实例、整套牌堆、角色 `MP`、轮转时间、抽牌行为等。
- [ ] 14. 评估是否需要扩展 Luban 技能表。
  - 单卡私有值继续走现有技能表，例如：副本数量、基础消耗值。
  - 牌堆公共规则改为新增独立配置表承载，例如：抽牌数量、出牌区上限、基础轮转时长、移动扣点倍率。
  - 技能对轮转时间、`MP` 消耗规则的修改参数，优先放在技能表或技能表关联结构中；基础牌堆规则不再塞进技能表。
  - 导表必须走 Unity `Game/Tool/ExcelExporter`。
- [ ] 15. 为技能消耗增加运行时覆盖机制。
  - 建议在 `GameplayAbilitySpec` 或独立卡牌运行时组件中缓存“基础消耗值 + 当前覆盖消耗值”。
  - 运行时修改只改覆盖值或最终结算值，不回写 Luban 生成表对象。
  - 当前明确运行时修改按“单张卡实例”生效，不影响同技能的其他副本，也不影响角色全局。
  - 遗物也可以成为消耗覆盖的来源之一，但仍然只能改到卡实例运行时值，不直接改配置表。
  - 需要定义覆盖值的来源、叠加规则、清理时机，以及轮转/弃牌/重抽后是否保留。
- [ ] 16. 回归验证核心流程。
  - 玩家进场按配置完成首抽。
  - 技能释放按配表消耗值扣角色 `MP`。
  - 运行时修改技能消耗后，UI 展示、可释放校验、实际扣费保持一致。
  - 持续移动按配置速率扣 `MP`。
  - 停止移动后停止扣点。
  - 按配置轮转后重置 `MP` 并弃置出牌区，再重抽。
  - 抽牌区为空时弃牌区回洗。
  - 出牌区配置上限生效。
  - 被动技能卡能按预期进入牌堆并触发。
  - 遗物对 `MP`、抽牌、轮转、单卡消耗的修正能通过日志确认。
  - 被动技能卡打出后能挂入能力区，并按 2 秒节拍持续触发。

## 建议的实现落点

建议新增或优先改造以下位置：

- 运行时数据：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Card/`
- 热更系统逻辑：
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Card/`
- 遗物运行时：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Relic/`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Relic/`
- UI 适配：
  - `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UISkill/`
  - `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UISkill/`

## 建议的配置扩展

当前 `DRSkill` 只有基础字段：

- `Id`
- `IsAct`
- `Name`
- `Desc`
- `IconPath`

要支撑本玩法，建议拆成“技能表私有字段”和“牌堆规则表公共字段”两部分。

### 技能表私有字段

建议优先在现有技能表上补以下字段：

- `CardCopies`
  - 同一技能在牌堆中的副本数量。
- `CardBaseCostMp`
  - 该技能卡的基础 `MP` 消耗。

### 新增牌堆规则表

建议新增独立表，例如：

- `DRSkillCardRule`
- 或 `DRSkillDeckRule`

建议至少包含以下字段：

- `Id`
  - 规则 Id。
- `DrawCount`
  - 抽牌数量。
- `HandLimit`
  - 出牌区上限。
- `CycleSeconds`
  - 基础轮转时长。
- `MoveDrainMpPerSecond`
  - 移动时每秒扣除的 `MP`。
- `PassiveTriggerIntervalSeconds`
  - 被动技能卡进入能力区后的周期触发间隔。当前规则默认为每 2 秒触发一次，仍建议走配置。

### 新增遗物表

建议新增独立表，例如：

- `DRRelic`

建议至少包含以下字段：

- `Id`
  - 遗物 Id。
- `Name`
  - 遗物名。
- `Desc`
  - 遗物描述。
- `EffectType`
  - 遗物效果类型。
- `EffectValue`
  - 遗物效果数值。
- `TriggerType`
  - 触发时机，例如抽牌、出牌、轮转、移动、战斗开始。

### 新增独立战斗配置表

当前仓库里没有明显现成的客户端战斗配置表适合承载这套卡牌/遗物入口，建议新增独立表，例如：

- `DRBattleCardConfig`
- 或 `DRBattleConfig`

建议至少包含以下字段：

- `Id`
  - 战斗配置 Id。
- `SkillCardRuleId`
  - 当前战斗使用的牌堆规则表 Id。
- `RelicIds`
  - 当前战斗默认携带的遗物列表。
- `Desc`
  - 配置说明。

如后续需要，也可以继续补：

- `DiscardOnCycle`
  - 轮转时是否整体弃掉出牌区。
- `ResetMpToMaxOnCycle`
  - 轮转时是否直接回满 `MaxMp`。

### 配置读取约定

当前建议使用以下约定：

- 单卡私有值从 `DRSkill` 读取。
- 牌堆公共规则从新增的 `DRSkillCardRule` 读取。
- 遗物效果从新增的 `DRRelic` 读取。
- 单位初始化时，需要先从独立战斗配置解析当前应使用的 `SkillCardRuleId` 与 `RelicIds`。
- 技能组配置继续只负责单位拥有哪些主动/被动技能，不再承载牌堆规则和遗物列表。

### 推荐挂点

结合当前仓库现状：

- `DRHero` 已经持有 `ActiveSkill` / `PassiveSkill`
- `DRMonster` 已经持有 `ActiveSkill` / `PassiveSkill`
- `DRUnitAttribute` 更偏向数值成长，不适合再承载战斗入口规则

因此当前最建议的挂法是：

- 在 `DRHero` 新增 `BattleCardConfigId`
- `BattleCardConfigId` 指向新增的 `DRBattleCardConfig`
- `DRBattleCardConfig` 再持有：
  - `SkillCardRuleId`
  - `RelicIds`

这样初始化链路最直接：

1. `SkillUnitSystem` 先从 `Hero/Monster` 读主动技能和被动技能列表。
2. 玩家侧再从 `Hero` 读 `BattleCardConfigId`。
3. 再由 `DRBattleCardConfig` 解析当前牌堆规则和遗物列表；怪物继续走原有技能链路。

如果后续你希望一个英雄在不同战斗模式复用不同卡组入口，再把 `BattleCardConfigId` 从 `Hero` 抽到更高层战斗入口配置即可。

这个约定的好处是：

- 单卡配置和牌堆公共规则边界清晰。
- 技能卡、牌堆规则、遗物三层职责明确。
- 战斗入口配置与技能内容配置解耦，后续更容易支持不同战斗模式复用同一套技能表。
- 不需要在多张技能行上重复维护相同公共数值。
- 运行时不需要再引入“规则来源卡”这种约定。

## 具体实现清单

### 第一批：数据与运行时骨架

- [ ] A1. 新增牌区枚举与卡牌实例模型。
  - 建议文件：
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Card/SkillCardZone.cs`
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Card/SkillCardRuntime.cs`
  - `SkillCardRuntime` 建议包含：
    - `CardInstanceId`
    - `SkillId`
    - `Spec`
    - `Zone`
    - `BaseCostMp`
    - `OverrideCostMp`
    - `CurrentResolvedCostMp`
- [ ] A2. 新增牌堆组件。
  - 建议文件：
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Card/SkillCardDeckComponent.cs`
  - 建议持有：
    - 抽牌区列表
    - 出牌区列表
    - 能力区列表
    - 弃牌区列表
    - 销毁区列表
    - 当前轮转剩余时间
    - 当前移动累计时间
    - 当前牌堆规则 Id
    - 被动技能周期触发累计时间
- [ ] A3. 新增牌堆系统逻辑。
  - 建议文件：
    - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Card/SkillCardDeckComponentSystem.cs`
  - 提供接口：
    - 初始化卡组
    - 抽牌
    - 挂入能力区
    - 弃牌
    - 洗牌回收
    - 销毁
    - 轮转 Tick
    - 移动扣点 Tick
    - 被动技能能力区 Tick
    - 从卡实例触发施法
- [ ] A4. 新增遗物组件与遗物实例模型。
  - 建议文件：
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Relic/RelicRuntime.cs`
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Relic/RelicContainerComponent.cs`
    - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Relic/RelicContainerComponentSystem.cs`
  - 提供接口：
    - 初始化遗物
    - 注册遗物触发
    - 查询当前遗物修正
    - 响应抽牌、出牌、轮转、移动、战斗开始等事件

### 第二批：技能表与授予链路接入

- [x] B1. 扩现有技能表并生成代码。
  - 目标：
    - 在 `DRSkill` 上生成卡牌玩法所需字段。
    - 在 `DRHero` 上补 `BattleCardConfigId`。
    - 新增 `DRBattleCardConfig`、`DRSkillCardRule`、`DRRelic`。
  - 验证：
    - Unity `Game/Tool/ExcelExporter`
    - Console 出现 `Luban excel export success!`
- [x] B2. 调整 `SkillUnitSystem`。
  - 目标文件：
    - `Assets/Scripts/Game/ET/Code/HotfixView/Client/Skill/Runtime/Battle/SkillUnitSystem.cs`
  - 改动方向：
    - 主动/被动技能都仍然 `GrantAbility`
    - 但不再直接视为 UI 可出牌列表或自动激活列表
    - 根据 `CardCopies` 生成多张卡实例并塞入抽牌区
    - 读取角色/单位技能组配置上的牌堆规则 Id
    - 从独立战斗配置初始化角色携带的遗物列表
- [ ] B3. 给 `GameplayAbilitySpec` 补最小运行时桥接字段或扩展方法。
  - 目标文件：
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/Skill/Runtime/Ability/GameplayAbilitySpec.cs`
    - 或其 `System` 扩展
  - 目标：
    - 允许通过 `Spec` 快速反查当前关联卡实例
    - 允许读取基础消耗与当前覆盖消耗

### 第三批：施法与 MP 结算

- [ ] C1. 统一技能卡施法入口。
  - 不再从 UI 直接 `asc.TryActivateAbility(spec)`
  - 改为 `deck.TryCastCard(cardInstanceId)`
  - 主动技能卡打出后按原有技能链路执行。
  - 被动技能卡打出后进入能力区，并按配置周期触发。
- [ ] C2. 接入 `MP` 校验与扣费。
  - 复用现有 `NumericType.Mp` / `NumericType.MaxMp`
  - 明确“卡牌消耗”和现有 Cost 节点的先后职责
  - 第一版建议：
    - 卡牌消耗作为唯一 `MP` 扣费入口
    - 若技能图里已有 `CostEffectNodeData` 扣 `MP`，则需要禁用或迁移，避免双扣
- [ ] C3. 实现单卡实例消耗覆盖。
  - 覆盖值只挂卡实例
  - 同技能其他副本保持原值
  - 弃牌/重抽后是否保留覆盖值，第一版建议“保留到该卡实例离开本场战斗”

### 第四批：轮转、移动扣点与 UI

- [ ] D1. 接入移动状态。
  - 第一版可先在玩法层同时参考：
    - 输入 `MoveValue`
    - 下层移动组件是否处于移动中
  - 最终以下层移动状态为准
- [ ] D2. 实现轮转。
  - 到时后：
    - `MP` 回满 `MaxMp`
    - 出牌区整体丢弃到弃牌区
    - 从抽牌区按配置数量补牌
    - 派发遗物触发事件
- [ ] D2.1 实现能力区驻留与被动周期触发。
  - 被动技能卡打出后进入能力区，不在普通轮转时立即进入弃牌区。
  - 能力区中的被动技能卡按 `PassiveTriggerIntervalSeconds` 周期触发。
  - 需要明确轮转时能力区是否清空；在未新增更多规则前，第一版建议能力区卡独立于出牌区轮转。
- [ ] D3. 改造技能 UI 数据源。
  - 目标文件：
    - `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UISkill/UIFormSkillComponent.cs`
    - `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UISkill/UIFormSkillComponentSystem.cs`
  - 改动方向：
    - `SkillSpecs` 改成卡牌视图列表
    - 保留对 `Spec` 的引用，仅用于展示技能名、图标和实际施法
- [ ] D4. 改造单格 UI。
  - 目标文件：
    - `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UISkill/SkillCellComponentSystem.cs`
  - 改动方向：
    - 展示当前卡实例消耗值
    - 展示角色当前 `MP`
    - 点击后走卡牌施法入口

## 第一阶段最小落地范围

如果要先做一个最小可玩的版本，建议只做下面这些：

1. 扩 `DRSkill`，至少补 `CardCopies`、`CardBaseCostMp`，并补能标识主动/被动卡牌语义所需的字段。
2. 新增 `DRSkillCardRule`，承载抽牌数量、出牌区上限、轮转时长、移动扣点等公共规则。
3. 新增 `DRRelic`，承载遗物基础效果。
4. 新增 `SkillCardRuntime`、`SkillCardDeckComponent`、`RelicContainerComponent`。
5. `SkillUnitSystem` 初始化主动/被动技能后，生成抽牌区卡实例并首抽，同时初始化遗物。
6. `UIFormSkillComponentSystem` 改为展示出牌区卡牌。
7. `SkillCellComponentSystem` 改为点击卡牌施法。
8. 施法成功后按卡实例当前消耗值扣 `MP`。
9. 实现最基础的轮转、移动扣点，以及被动技能卡进入能力区后的周期触发。
10. 打通一类最简单遗物效果，例如“减少单卡 `MP` 消耗”或“增加抽牌数”。

这个阶段先不做：

- 销毁区真实玩法入口
- 复杂 Buff 对轮转时间的叠加策略
- 消耗覆盖的复杂来源优先级
- 复杂被动技能触发图谱
- 复杂遗物连锁效果

## 风险与待确认项

### 风险

1. 当前技能 UI 默认按 `GrantedAbilities` 直接渲染，改成牌区后会影响编辑器烟测、按钮状态刷新和索引稳定性。
2. 当前技能消耗逻辑在 `GameplayAbilitySpec.CanAffordCost()` 里已经偏向通用属性消耗；既然当前明确复用 `MP`，则需要统一“技能卡玩法额外消耗”和现有 Cost 节点的职责，避免双重扣费。
3. 移动状态的“开始/停止”如果只看输入，会和寻路、被打断、服务器同步停止产生偏差，建议最终以下层移动组件状态为准。
4. 轮转时间允许被技能修改，意味着轮转计时器必须支持运行中重算、延长、缩短和 UI 实时刷新，不能做成简单固定间隔。
5. 同一技能允许多副本后，UI 点击、弃牌、洗牌、日志都必须按“卡牌实例”而不是 `SkillId` 追踪。
6. 如果运行时直接修改静态配置对象，会污染全局技能配置与其他副本实例；必须明确区分“基础配表值”和“战斗态覆盖值”。
7. 既然各种数值都走配置，就必须明确配置的作用域和优先级，避免角色配置、技能配置、效果节点配置互相覆盖后无法解释最终结果。
8. 被动技能卡牌化后，其“抽到时生效 / 出牌时生效 / 常驻生效”需要严格区分，否则会与原有被动自动激活语义冲突。
9. 遗物会和技能表、牌堆规则表、运行时覆盖值共同影响结算，优先级必须提前定义，否则后续调试成本会很高。

### 待确认

1. 轮转时间虽然当前默认是 2 秒，但允许被技能动态修改；需要进一步明确修改来源、叠加规则和持续时间。
2. 当前技能点已确认直接复用 `MP`，轮转重置时直接回满 `MaxMp`；后续只需明确相关配表入口和运行时修改来源。
3. 同一技能允许在牌堆中存在多张副本，需要进一步明确副本来源是单位初始配置、战斗中生成，还是两者都支持。
4. 被动技能卡当前明确按技能类型细分；当前默认规则为“打出后挂入能力区，每 2 秒触发一次”，后续仍需细化不同类型的差异化触发方式。
5. 遗物列表当前明确挂在独立战斗配置。
6. 销毁区第一版是否需要真实入口，还是只保留数据结构。

## 完成标准

1. 主动技能和被动技能都不再沿用旧的直接授予后立即生效逻辑，而是通过卡组与牌区规则进入运行时。
2. UI 展示的数据源明确来自技能卡牌运行时，而不是原始 `GrantedAbilities`。
3. 技能释放、移动扣点、轮转重置、洗牌回收四条链路都能通过日志确认。
4. 遗物系统能完成初始化、触发和数值修正验证。
5. 关键流程编译通过，且在本地技能场景中可完成首抽、施法、移动扣点、轮转弃牌与回洗验证。
