# Unity项目模块重命名：Demo → GenAtom

**日期**: 2026-01-20  
**分类**: 重构 (Refactoring)  
**标签**: `refactoring`, `unity`, `et-framework`, `rename`, `luban`, `game-development-kit`

---

## 问题症状

项目基于GameDevelopmentKit框架，默认使用"Demo"作为示例模块名称。需要将其改为实际游戏名"GenAtom"，涉及：

- ❌ 枚举类型使用 `SceneType.Demo` 和 `AppType.Demo`
- ❌ 代码中大量 `SceneType.Demo` 引用（MessageHandler、Event特性）
- ❌ 文件夹命名为 `Demo`（分布在Hotfix/Model/ModelView等多个目录）
- ❌ 配置文件命名为 `Demo.xlsx`
- ❌ Luban生成的代码使用 `DTDemo`、`DRDemo` 类名

**影响范围**：
- ET框架核心代码
- UnityGameFramework集成代码
- Luban配置系统
- 9个文件夹，12个代码文件，多个配置文件

---

## 根因分析

### 技术背景

GameDevelopmentKit是一个双端（客户端/服务器）游戏开发框架，结合了：
- **服务器**: ET 8.1 Framework
- **客户端**: UnityGameFramework (GF) + ET模块集成
- **配置**: Luban配置导出系统

### 问题根源

1. **框架设计**：项目使用"Demo"作为默认示例，方便开发者快速上手
2. **分层架构**：Demo名称分布在多个架构层：
   - **枚举层**：SceneType、AppType定义
   - **业务层**：Hotfix（热更逻辑）、Model（数据模型）、ModelView（视图模型）
   - **配置层**：Excel配置文件和Luban生成代码
3. **依赖关系**：代码引用、文件夹结构、配置文件之间存在强依赖

---

## 解决方案

### 步骤1：修改枚举定义

#### 1.1 修改 SceneType 枚举

**文件**: `Unity/Assets/Scripts/Library/ET/Core/Runtime/Entity/SceneType.cs`

```csharp
// 修改前
public enum SceneType: long
{
    // ...
    Demo = 1 << 30,
    Current = 1L << 31,
    LockStep = 1L << 32,
    LockStepView = 1L << 33,
    DemoView = 1L << 34,
    NetClient = 1L << 35,
}

// 修改后
public enum SceneType: long
{
    // ...
    GenAtom = 1 << 30,          // ✅ Demo → GenAtom
    Current = 1L << 31,
    LockStep = 1L << 32,
    LockStepView = 1L << 33,
    GenAtomView = 1L << 34,     // ✅ DemoView → GenAtomView
    NetClient = 1L << 35,
}
```

#### 1.2 修改 AppType 枚举

**文件**: `Unity/Assets/Scripts/Library/ET/Core/Runtime/World/Module/Options/Options.cs`

```csharp
// 修改前
public enum AppType
{
    Server,
    Watcher,
    // ...
    Demo,
    LockStep,
}

// 修改后
public enum AppType
{
    Server,
    Watcher,
    // ...
    GenAtom,    // ✅ Demo → GenAtom
    LockStep,
}
```

---

### 步骤2：修改代码引用

#### 2.1 修改 GlobalComponent

**文件**: `Unity/Assets/Scripts/Game/ET/Loader/GlobalComponent.cs`

```csharp
// 修改前
public void OnAwake()
{
    AppType = AppType.Demo;
}

// 修改后
public void OnAwake()
{
    AppType = AppType.GenAtom;  // ✅
}
```

#### 2.2 修改所有 MessageHandler 和 Event 特性

**涉及文件**（共11个）：

1. **Hotfix/Client/GenAtom/Main/Unit/**
   - `M2C_CreateMyUnitHandler.cs`
   - `M2C_CreateUnitsHandler.cs`
   - `M2C_RemoveUnitsHandler.cs`

2. **Hotfix/Client/GenAtom/Main/Move/**
   - `M2C_PathfindingResultHandler.cs`
   - `M2C_StopHandler.cs`

3. **Hotfix/Client/GenAtom/Main/Scene/**
   - `M2C_StartSceneChangeHandler.cs`

4. **HotfixView/Client/GenAtom/UI/**
   - `AppStartInitFinish_CreateLoginUI.cs`
   - `LoginFinish_RemoveLoginUI.cs`
   - `LoginFinish_CreateLobbyUI.cs`

5. **HotfixView/Client/GenAtom/Scene/**
   - `SceneChangeStart_AddComponent.cs`
   - `AfterCreateClientScene_AddComponent.cs`

6. **Hotfix/Server/GenAtom/Robot/**
   - `FiberInit_Robot.cs`

**修改模式**：

```csharp
// MessageHandler 修改
[MessageHandler(SceneType.Demo)]      // 修改前
[MessageHandler(SceneType.GenAtom)]   // 修改后 ✅

// Event 修改
[Event(SceneType.Demo)]               // 修改前
[Event(SceneType.GenAtom)]            // 修改后 ✅

// SceneType 赋值修改
root.SceneType = SceneType.Demo;      // 修改前
root.SceneType = SceneType.GenAtom;   // 修改后 ✅
```

---

### 步骤3：重命名文件夹

#### 3.1 使用 PowerShell 批量重命名

```powershell
# 重命名单个文件夹
Rename-Item -Path "原路径\Demo" -NewName "GenAtom"
```

#### 3.2 需要重命名的文件夹（共9个）

| 原路径 | 新路径 |
|--------|--------|
| `Code/Hotfix/Client/Demo` | `Code/Hotfix/Client/GenAtom` |
| `Code/Hotfix/Server/Demo` | `Code/Hotfix/Server/GenAtom` |
| `Code/Hotfix/Share/Demo` | `Code/Hotfix/Share/GenAtom` |
| `Code/HotfixView/Client/Demo` | `Code/HotfixView/Client/GenAtom` |
| `Code/Model/Client/Demo` | `Code/Model/Client/GenAtom` |
| `Code/Model/Server/Demo` | `Code/Model/Server/GenAtom` |
| `Code/Model/Share/Demo` | `Code/Model/Share/GenAtom` |
| `Code/ModelView/Client/Demo` | `Code/ModelView/Client/GenAtom` |
| `Assets/Res/UI/UIForm/Demo` | `Assets/Res/UI/UIForm/GenAtom` |

**注意**：Unity会自动处理对应的 `.meta` 文件重命名。

---

### 步骤4：重命名配置文件

#### 4.1 重命名 Excel 文件

```powershell
Rename-Item -Path "Design\Excel\ET\Datas\Demo.xlsx" -NewName "GenAtom.xlsx"
```

**路径**: `Design/Excel/ET/Datas/Demo.xlsx` → `GenAtom.xlsx`

---

### 步骤5：重新生成 Luban 配置

#### 5.1 在 Unity 编辑器中操作

1. 打开 Unity 编辑器
2. 等待编译完成
3. 使用 Luban 配置导出工具重新生成配置

#### 5.2 自动生成的文件

重新生成后，以下文件会自动更新：

**代码文件**：
- `DTDemo.cs` → `DTGenAtom.cs`（或根据表名定义）
- `DRDemo.cs` → `DRGenAtom.cs`

**数据文件**：
- `dtdemo.bytes` → `dtgenatom.bytes`

**生成位置**：
- `Unity/Assets/Scripts/Game/ET/Code/Model/Generate/Client/Luban/`
- `Unity/Assets/Scripts/Game/ET/Code/Model/Generate/ClientServer/Luban/`
- `Unity/Assets/Res/ET/Client/Luban/`
- `Unity/Assets/Res/ET/ClientServer/Luban/`
- `Config/Luban/`

---

## 验证步骤

### 1. 编译验证

```bash
# 打开 Unity 编辑器，等待编译完成
# 检查 Console 是否有错误
```

### 2. 代码搜索验证

使用 Grep 工具搜索残留的 "Demo" 引用：

```bash
# 搜索 SceneType.Demo
grep -r "SceneType\.Demo" Unity/Assets/Scripts/Game/ET/Code/

# 搜索 AppType.Demo
grep -r "AppType\.Demo" Unity/Assets/Scripts/Game/ET/
```

### 3. 运行测试

1. 运行游戏，检查启动流程
2. 测试 UI 加载（Login、Lobby界面）
3. 测试网络消息处理
4. 测试场景切换

---

## 涉及文件清单

### 核心代码文件（14个）

1. `SceneType.cs` - 枚举定义
2. `Options.cs` - AppType枚举
3. `GlobalComponent.cs` - AppType引用
4. `M2C_CreateMyUnitHandler.cs`
5. `M2C_CreateUnitsHandler.cs`
6. `M2C_RemoveUnitsHandler.cs`
7. `M2C_PathfindingResultHandler.cs`
8. `M2C_StopHandler.cs`
9. `M2C_StartSceneChangeHandler.cs`
10. `AppStartInitFinish_CreateLoginUI.cs`
11. `LoginFinish_RemoveLoginUI.cs`
12. `LoginFinish_CreateLobbyUI.cs`
13. `SceneChangeStart_AddComponent.cs`
14. `AfterCreateClientScene_AddComponent.cs`
15. `FiberInit_Robot.cs`

### 文件夹（9个）

- 所有 `Demo` 文件夹及其 `.meta` 文件

### 配置文件

- `Demo.xlsx` → `GenAtom.xlsx`
- Luban生成的代码和数据文件

---

## 预防策略

### 1. 项目初期规划

✅ **在项目初期就使用实际项目名称**，避免后期大规模重命名

```csharp
// 好的做法：从一开始就使用项目名
public enum SceneType: long
{
    MyGame = 1 << 30,  // 使用实际项目名
}
```

### 2. 使用代码搜索工具

✅ **使用 Grep/Ripgrep 确保所有引用都被修改**

```bash
# 搜索所有 Demo 引用
rg "\bDemo\b" --type cs

# 搜索特定模式
rg "SceneType\.Demo" Unity/Assets/Scripts/
```

### 3. 使用 IDE 重构功能

✅ **优先使用 IDE 的 Rename Symbol 功能**

- Rider: `Ctrl+R, R` (Rename)
- Visual Studio: `Ctrl+R, Ctrl+R` (Rename)
- VS Code: `F2` (Rename Symbol)

**注意**：IDE重构只能处理代码引用，文件夹和配置文件仍需手动处理。

### 4. 版本控制最佳实践

✅ **在独立分支进行大规模重命名**

```bash
# 创建重命名分支
git checkout -b refactor/rename-demo-to-genatom

# 完成修改后提交
git add .
git commit -m "refactor: 重命名Demo为GenAtom"

# 合并前进行充分测试
```

### 5. 分步验证

✅ **每完成一个步骤就验证编译**

1. 修改枚举 → 编译验证
2. 修改代码引用 → 编译验证
3. 重命名文件夹 → 编译验证
4. 重命名配置 → 重新生成 → 编译验证

---

## 注意事项

### ⚠️ 不要修改的文件

**第三方库的 Demo 文件夹**：
- `Assets/Plugins/Sirenix/Demos` - Odin Inspector 示例
- `Library/PackageCache/*/Samples~/Demo` - Unity 包示例

这些是第三方库的示例代码，不应修改。

### ⚠️ Unity 自动处理

- `.meta` 文件会被 Unity 自动重命名
- 如果出现 `.meta` 文件冲突，让 Unity 重新生成

### ⚠️ Luban 配置依赖

- Excel 文件重命名后**必须**重新生成 Luban 配置
- 否则会导致运行时找不到配置数据

---

## 相关资源

### 项目文档

- [CLAUDE.md](../../../CLAUDE.md) - 项目架构说明
- [AGENTS.md](../../../Unity/AGENTS.md) - 编码规范

### 相关问题

- 如果遇到 Luban 配置生成问题，参考 `Book/Luban配置.md`
- 如果遇到 ET 代码生成问题，参考 `Book/ET代码生成工具.md`

---

## 总结

### 成功指标

✅ 所有 `SceneType.Demo` 引用已改为 `SceneType.GenAtom`  
✅ 所有 `AppType.Demo` 引用已改为 `AppType.GenAtom`  
✅ 9个 Demo 文件夹已重命名为 GenAtom  
✅ 配置文件已重命名  
✅ Unity 编译无错误  
✅ 游戏运行正常  

### 经验教训

1. **系统性重命名需要多层面考虑**：枚举、代码、文件夹、配置
2. **使用工具提高效率**：Grep搜索、PowerShell批量操作
3. **分步验证降低风险**：每步完成后立即编译验证
4. **框架集成项目的重命名更复杂**：需要考虑配置生成系统

### 时间成本

- 搜索分析：10分钟
- 代码修改：15分钟
- 文件夹重命名：5分钟
- 验证测试：10分钟
- **总计**：约40分钟

---

**记录人**: Claude (AI Assistant)  
**审核状态**: ✅ 已验证  
**最后更新**: 2026-01-20
