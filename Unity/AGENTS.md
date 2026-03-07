# *MUST*使用中文回答用户问题
---

## Plan Completion Rule

执行 `plans/*.md` 中的任务时，完成每个任务后**必须**用 Edit 工具将对应的 `- [ ]` 改为 `- [x]`，保持计划文件与实际进度同步。

---

## Compound Engineering Workflows

使用 Factory 自定义命令实现工程工作流。命令文件位于 `.factory/commands/` 目录。

**可用命令**：
- `/workflows-plan <功能描述>` - 将功能描述转换为结构化计划文档
- `/workflows-work [计划文件]` - 执行计划文件中的任务
- `/workflows-review [目标]` - 多角度代码审查
- `/workflows-compound [问题描述]` - 记录解决的问题，积累知识

---

**核心理念**：`Plan → Work → Review → Compound → Repeat`

**80/20 原则**：80% 在规划和审查，20% 在执行。

---

## AI 执行任务 Do / Don't

### 架构速记

- 当前目录是 **Unity 客户端工程根目录**。
- 整体架构：**UnityGameFramework (GF) + ET 8.1 + HybridCLR + UniTask + Luban + CodeBind**。
- 速记：**GF 负责表现与资源承载，ET 负责组件化业务逻辑，HybridCLR 负责热更，Luban / CodeBind 负责生成与绑定。**

### Do

- 先判断任务属于哪条链路，再决定改哪里。
- 做 **热更玩法 / UI / 流程 / 配置消费逻辑** 时，优先看 `Assets/Scripts/Game/Hot/Code/`。
- 做 **Hot 启动、桥接、基础 MonoBehaviour、网络包装** 时，优先看 `Assets/Scripts/Game/Hot/Loader/`。
- 做 **ET 组件、系统、事件、实体逻辑** 时，优先看 `Assets/Scripts/Game/ET/Code/`。
- 做 **GF 与 ET 生命周期桥接** 时，优先看 `Assets/Scripts/Game/ET/Loader/UGF/`。
- 默认优先改 `Assets/Scripts/Game/`，只有确认必须修改底层能力时才改 `Assets/Scripts/Library/`。
- ET 分层时，优先遵守：`Model / ModelView` 放数据与表现绑定，`Hotfix / HotfixView` 放系统逻辑与运行时行为。
- 现有 UI 如果走 `AHotUIForm` / MonoBehaviour 路线，就继续按 **GameHot UI** 模式实现。
- 现有 UI 如果走 `UGFUIForm` / `UIComponent` / `[UGFUIFormSystem]` 路线，就继续按 **ETUI** 模式实现。
- 现有 Entity 如果走 `EntityLogic` / `AHotEntity` 路线，就继续按 **GameHot Entity** 模式实现。
- 现有 Entity 如果走 `UGFEntity` + ET 组件 / 系统路线，就继续按 **ET + UGFEntity** 模式实现。
- 修改 UI / Entity / Scene / 配置相关功能时，同时检查 `Assets/Res/` 下对应资源与命名是否匹配。
- 遇到 `Game/Generate/`、`Hot/Code/Generate/`、`*.Bind.cs`、Luban / Proto 文件时，先判断它是不是生成产物，并追溯上游来源。
- 运行时代码放在运行时目录；编辑器工具、构建流程、导出工具放在 `*/Editor/`。
- 新业务代码默认一个文件只放一个类；只有项目既有模式或生成代码明确要求时，才允许一个文件内放多个类。

### Don't

- 不要先改 `Assets/Scripts/Library/`，除非已确认业务层无法解决。
- 不要把频繁变化的业务逻辑塞进 `Assets/Scripts/Game/Hot/Loader/`。
- 不要把运行时代码放进 `*/Editor/`。
- 不要在同一个 UI 功能里混用 **GameHot UI** 和 **ETUI** 两套工作流，除非任务明确要求做桥接改造。
- 不要在同一个 Entity 功能里混用两套不一致的实体实现路径。
- 不要手改生成文件，除非任务明确要求，或已确认项目约定允许这样做。
- 不要只改代码不看资源；凡是依赖 UI、Entity、Scene、配置表、热更资源的改动，都必须检查 `Assets/Res/`。
- 不要自行发明新的 entity 引用持有方式；优先复用项目里已有封装和约定。
- 新业务代码中不要将多个类写入同一个文件，除非明确是在维护既有合并文件或生成产物。

---

## Share/Analyzer 约束速记

### 适用范围

- ET 分析器主要约束 `Game.ET.Code.Model`、`Game.ET.Code.ModelView`、`Game.ET.Code.Hotfix`、`Game.ET.Code.HotfixView`，以及对应 DotNet 的 `Model` / `Hotfix` 程序集。
- Custom 分析器会额外约束 `Game`、`Game.Hot.Code`、`Game.Hot.Loader` 等 Unity 客户端程序集。

### Entity / Model 约束

- 在 `Model` / `ModelView` 程序集中，默认不要声明普通类；优先声明 ET `Entity` / `LSEntity` / `Object` 体系类型。确实需要普通类时，使用 `[EnableClass]`。
- Entity / LSEntity 优先 **直接继承** `ET.Entity` 或 `ET.LSEntity`，不要做多层实体继承，也不要声明泛型实体类。
- Entity 类默认 **不要在类内直接写业务方法**；业务逻辑优先写到对应的 `System` 静态类。确有必要时，使用 `[EnableMethod]`。
- Entity 类不要声明委托字段或属性。
- Entity 类不要声明实体字段、实体数组、或包含实体类型参数的泛型字段；**统一优先使用 `EntityRef<>` / `EntityWeakRef<>`**。
- `LSEntity` 不要声明 `float` / `double` 字段、属性，或包含浮点类型参数的泛型成员。
- 一个实体不要同时标记 `[ComponentOf]` 和 `[ChildOf]`。
- `IMessage` 消息类不要声明实体类型字段。

### Entity 访问与生命周期约束

- 不要在实体外部直接访问实体实例字段；只有实体自身或带 `[FriendOf]` 的类可以直接访问。
- 使用 `AddChild` / `AddChildWithId` 时，child 类型必须满足 `[ChildOf]` 约束。
- 使用 `AddComponent` / `GetComponent` 时，component 类型必须满足 `[ComponentOf]` 约束。
- 不要在 `Entity` / `LSEntity` 基类上下文直接访问 `Child` / `Component`；确需这样做时，使用 `[EnableAccessEntiyChild]`。
- `EntitySystem` / `LSEntitySystem` 方法必须放在带 `[EntitySystemOf]` / `[LSEntitySystemOf]` 的静态类中，并保持生命周期函数与生成系统一致。

### Hotfix / ET 代码放置约束

- ET 的 `Hotfix` / `HotfixView` 程序集中，优先声明静态系统类，或声明带 `BaseAttribute` 子类特性的类型。
- ET 的 `Hotfix` / `HotfixView` 程序集中，不要声明非 `const` 字段和属性；状态应尽量放回 `Model` / `ModelView` 的实体或组件中。

### 通用编码约束

- 异步统一使用 `UniTask` / `UniTaskVoid`，不要使用 `Task` 或 `async void`。
- 同步方法里调用 `UniTask` 返回函数时，要补 `.Forget()`；异步方法里要么 `await`，要么显式 `.Forget()`。
- 运行时代码记日志时，优先使用 `UnityGameFramework.Runtime.Log`，不要直接用 `UnityEngine.Debug.Log*`。
- 字符串拼接优先使用 `GameFramework.Utility.Text.Format`，不要使用 `+`、`string.Format`、`string.Concat`。
- 命名遵循分析器约束：类型 / 公有成员 / 常量首字母大写；私有 / 保护成员、局部变量首字母小写；名称不要以下划线结尾。
- 静态字段或静态属性默认需要标记 `[StaticField]`。
- 带 `[UniqueId]` 约束的常量 ID 必须落在指定范围内，且不能重复。
