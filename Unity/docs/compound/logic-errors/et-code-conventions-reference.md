# ET 8.1 代码规范参考手册

## 问题症状

在ET框架中编写新功能时，经常因不熟悉代码规范导致：
- 编译错误（缺少特性标记、类修饰符错误）
- 运行时System未注册（生命周期方法签名不对）
- 热更新失效（代码放错层级目录）
- UI/Entity生命周期回调不触发（使用了错误的System特性标记）

## 根因分析

ET 8.1 采用 Source Generator + 特性标记驱动的ECS架构，有严格的代码组织和命名约定。不遵循这些约定会导致代码生成器无法正确生成胶水代码，System无法被框架发现和调用。

## 解决方案：ET代码规范速查

### 1. 四层分离架构

| 层 | 目录 | 职责 | 可热更 |
|---|---|---|---|
| Model | `ET/Code/Model/` | Entity/Component数据定义 | ❌ |
| ModelView | `ET/Code/ModelView/` | 依赖Unity的Entity/Component定义 | ❌ |
| Hotfix | `ET/Code/Hotfix/` | 纯逻辑System、消息处理、事件处理 | ✅ |
| HotfixView | `ET/Code/HotfixView/` | 依赖Unity API的视图逻辑System | ✅ |

每层再按 `Client/Server/Share` 分端：
- `Share/` - 客户端服务端共享代码，namespace `ET`
- `Client/` - 客户端专用，namespace `ET.Client`
- `Server/` - 服务端专用，namespace `ET.Server`

### 2. Entity/Component 定义（Model/ModelView层）

```csharp
// 作为某Entity的Component
[ComponentOf(typeof(Unit))]
public class NumericComponent : Entity, IAwake, IDestroy
{
    // 只放数据字段，不放逻辑
    public Dictionary<int, long> NumericDic = new();
}

// 作为某Entity的Child
[ChildOf(typeof(UnitComponent))]
public partial class Unit : Entity, IAwake<int>
{
    public int ConfigId { get; set; }
}

// 无约束Component
[ComponentOf]
public sealed class DynamicEventComponent : Entity, IAwake, IDestroy { }
```

生命周期接口：
- `IAwake` / `IAwake<A>` / `IAwake<A,B>` / `IAwake<A,B,C>` - 创建时
- `IDestroy` - 销毁时
- `IUpdate` - 每帧更新
- `ILateUpdate` - 每帧延迟更新
- `ITransfer` - 可传输
- `ISerializeToEntity` - 序列化

### 3. System 实现（Hotfix/HotfixView层）

```csharp
[EntitySystemOf(typeof(MoveComponent))]   // 必须：声明目标Entity
[FriendOf(typeof(MoveComponent))]          // 可选：访问私有字段
public static partial class MoveComponentSystem  // 必须：static partial
{
    [EntitySystem]                          // 必须：标记生命周期方法
    private static void Awake(this MoveComponent self)
    {
        // 初始化
    }

    [EntitySystem]
    private static void Destroy(this MoveComponent self)
    {
        // 销毁
    }

    [EntitySystem]
    private static void Update(this MoveComponent self)
    {
        // 每帧
    }

    // 业务扩展方法（public/private均可）
    public static bool IsArrived(this MoveComponent self)
    {
        return self.Targets.Count == 0;
    }
}
```

**关键规则**：
- 类名：`{EntityName}System`
- 修饰符：`public static partial class`
- 生命周期方法：`[EntitySystem]` + `private static void` + 扩展方法形式
- Awake支持多参数重载

### 4. 事件系统

```csharp
// Model层 - 定义事件结构体
public struct ChangePosition
{
    public Unit Unit;
    public float3 OldPos;
}

// Hotfix层 - 订阅事件
[Event(SceneType.Current)]  // 指定Scene类型
public class ChangePosition_SyncGameObjectPos : AEvent<Scene, ChangePosition>
{
    protected override async UniTask Run(Scene scene, ChangePosition args)
    {
        // 处理逻辑
    }
}

// 发布事件
EventSystem.Instance.Publish(self.Scene(), new ChangePosition() { ... });
await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
```

命名规范：`{事件名}_{处理描述}`

### 5. 消息处理器

```csharp
[MessageHandler(SceneType.GenAtom)]
public class M2C_CreateMyUnitHandler : MessageHandler<Scene, M2C_CreateMyUnit>
{
    protected override async UniTask Run(Scene root, M2C_CreateMyUnit message)
    {
        // 处理
    }
}
```

命名规范：`{消息名}Handler`

### 6. Timer

```csharp
// 嵌套在System类内部
[EntitySystemOf(typeof(AIComponent))]
public static partial class AIComponentSystem
{
    [Invoke(TimerInvokeType.AITimer)]
    public class AITimer : ATimer<AIComponent>
    {
        protected override void Run(AIComponent self)
        {
            self.Check();
        }
    }
}
```

### 7. Singleton

```csharp
// World级别Singleton
World.Instance.AddSingleton<IdGenerater>();

// [Code] 可热更Singleton
[Code]
public class NumericWatcherComponent : Singleton<NumericWatcherComponent>, ISingletonAwake
{
    public void Awake() { }
}
```

### 8. ETUI 集成（本项目特有）

**ModelView层**：
```csharp
// Mono脚本（挂预制体）
[MonoCodeBind]
public partial class MonoUIFormLogin : AETMonoUGFUIForm { }

// ET Component
[ComponentOf(typeof(UIComponent))]
public class UIFormLoginComponent : UGFUIForm<MonoUIFormLogin>, 
    IAwake, IUGFUIFormOnInit, IUGFUIFormOnOpen, IUGFUIFormOnClose
{ }
```

**HotfixView层**：
```csharp
[EntitySystemOf(typeof(UIFormLoginComponent))]
[FriendOf(typeof(UIFormLoginComponent))]
public static partial class UIFormLoginComponentSystem
{
    [UGFUIFormSystem]  // ⚠️ UI用 [UGFUIFormSystem]，不是 [EntitySystem]
    private static void UGFUIFormOnInit(this UIFormLoginComponent self) { }

    [UGFUIFormSystem]
    private static void UGFUIFormOnOpen(this UIFormLoginComponent self) { }

    [UGFUIFormSystem]
    private static void UGFUIFormOnClose(this UIFormLoginComponent self, bool isShutdown) { }
}
```

UI生命周期接口：`IUGFUIFormOnInit`, `IUGFUIFormOnOpen`, `IUGFUIFormOnClose`, `IUGFUIFormOnPause`, `IUGFUIFormOnResume`, `IUGFUIFormOnCover`, `IUGFUIFormOnReveal`, `IUGFUIFormOnRefocus`, `IUGFUIFormOnUpdate`, `IUGFUIFormOnDepthChanged`, `IUGFUIFormOnRecycle`

### 9. ETEntity 集成

**ModelView层**：
```csharp
[MonoCodeBind]
public partial class EntityTest : AETMonoUGFEntity { }

[EnableMethod]
public class UGFEntityTest : UGFEntity<EntityTest>, IUGFEntityOnShow { }
```

**HotfixView层**：
```csharp
[EntitySystemOf(typeof(UGFEntityTest))]
public static partial class UGFEntityTestSystem
{
    [UGFEntitySystem]  // ⚠️ Entity用 [UGFEntitySystem]
    private static void UGFEntityOnShow(this UGFEntityTest self) { }
}
```

Entity生命周期接口：`IUGFEntityOnInit`, `IUGFEntityOnShow`, `IUGFEntityOnHide`, `IUGFEntityOnUpdate`, `IUGFEntityOnRecycle`, `IUGFEntityOnAttachTo`, `IUGFEntityOnAttached`, `IUGFEntityOnDetachFrom`, `IUGFEntityOnDetached`

### 10. UIWidget 集成

**ModelView层**：
```csharp
[ComponentOf(typeof(UIFormLoginComponent))]
public class UIWidgetTest : UGFUIWidget<MonoUIWidgetTest>, 
    IAwake, IUGFUIWidgetOnInit, IUGFUIWidgetOnOpen
{ }
```

**HotfixView层**：
```csharp
[EntitySystemOf(typeof(UIWidgetTest))]
public static partial class UIWidgetTestSystem
{
    [UGFUIWidgetSystem]  // ⚠️ Widget用 [UGFUIWidgetSystem]
    private static void UGFUIWidgetOnInit(this UIWidgetTest self) { }
}
```

### 11. FiberInit 入口

```csharp
[Invoke((long)SceneType.Main)]
public class FiberInit_Main : AInvokeHandler<FiberInit, UniTask>
{
    public override async UniTask Handle(FiberInit fiberInit)
    {
        Scene root = fiberInit.Fiber.Root;
        await EventSystem.Instance.PublishAsync(root, new EntryEvent1());
        await EventSystem.Instance.PublishAsync(root, new EntryEvent2());
        await EventSystem.Instance.PublishAsync(root, new EntryEvent3());
    }
}
```

### 12. 常用API速查

```csharp
self.Root()                          // 获取Fiber根Scene
self.Scene()                         // 获取所属Scene
self.Fiber()                         // 获取所属Fiber
self.GetParent<T>()                  // 获取父Entity
self.GetComponent<T>()               // 获取Component
self.AddComponent<T>()               // 添加Component
self.AddChild<T>()                   // 添加Child
self.RemoveComponent<T>()            // 移除Component
self.RemoveChild(id)                 // 移除Child
EntityRef<T>                         // 弱引用（跨Entity引用必须用）
using var list = ListComponent<T>.Create()  // 对象池List
```

## 涉及文件

- `Library/ET/Core/Runtime/Entity/` - Entity基类和生命周期接口定义
- `Library/ET/Core/Runtime/Fiber/EntitySystem.cs` - System调度器
- `Library/ET/Core/Runtime/World/Singleton.cs` - Singleton基类
- `Game/ET/Code/Model/` - 业务Entity/Component定义
- `Game/ET/Code/Hotfix/` - 业务System实现
- `Game/ET/Code/ModelView/` - 视图层Entity定义
- `Game/ET/Code/HotfixView/` - 视图层System实现
- `Game/ET/Loader/UGF/UIForm/` - ETUI框架
- `Game/ET/Loader/UGF/Entity/` - ETEntity框架
- `Game/ET/Loader/UGF/UIWidget/` - UIWidget框架

## 预防策略

1. 新建文件前先确认应放在哪一层（Model/ModelView/Hotfix/HotfixView）
2. 注意三种不同的System特性标记：`[EntitySystem]`、`[UGFUIFormSystem]`、`[UGFEntitySystem]`、`[UGFUIWidgetSystem]`
3. System类必须是 `public static partial class`
4. Entity类只放数据，逻辑全部写在对应的System扩展方法中
5. 跨Entity引用使用 `EntityRef<T>`，避免直接持有引用导致内存泄漏
6. 异步统一使用 `UniTask`，不使用 `Task` 或 `ETTask`

## 标签

`ET框架` `ECS` `代码规范` `热更新` `ETUI` `ETEntity` `Source Generator` `四层架构`
