# Voronoi Biome 纯地形 MVP

## 问题陈述

在当前 `nmap` 链路上，先完成“只生成地形、不放资源”的随机地图 MVP。目标是基于 Voronoi 和 `ET.Biome` 生成稳定的海洋、陆地、内陆水域与基础地表分层，为后续资源投放提供可靠地形底座。

## 背景与动机

- 当前工程已经具备 `BiomeMap -> MapGraph -> Biome -> DrawMap` 的基础链路，但地形层划分和渲染层配置仍需收敛。
- 现阶段不引入任何资源生成逻辑，避免把问题耦合到 prefab/entity 投放。
- 先把 biome 驱动的纯地形做稳定，后续资源只需读取 `MapCenter.biome` 即可扩展。

## 验收标准

- [x] `MapGraph` 的海洋/水域初始判定与后续 `Biome` 推导语义一致
- [x] `GenMapSystem` 能稳定生成随机岛屿地图，并把 `MapGraph` 交给地形渲染链路
- [x] `DrawMapSystem` 按 biome 输出纯地形分层，不生成任何资源实体
- [x] `Map.prefab` 与 `DrawCarpet` 图层数量、顺序、贴图配置一致
- [x] Unity 编译通过，且工作区仅保留与本任务无关的既有改动

## 涉及文件

- `Assets/Scripts/Library/ET/Core/Runtime/Third/nmap/Map/MapGraph.cs`
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/nmap/Tools/GenMapSystem.cs`
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/nmap/Tools/DrawMapSystem.cs`
- `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/nmap/Tools/DrawCarpet.cs`
- `Assets/Res/Prefab/map/Map.prefab`

## 技术考量

- `Biome` 是地形语义源头，`MapNode`/地表层只负责显示，不反向决定 biome。
- 地表层按“海洋 / 干旱裸地 / 绿色植被 / 内陆水冷地表”拆分，避免一层承担过多混合逻辑。
- 保留当前 `ApplyClusterBiomes` 作为连续大块 biome 的主要策略，不新增资源规则。
- 优先复用现有 `DrawCarpet` 与 `Map` 预制体结构，避免新建额外桥接层。

## 风险与依赖

- `Map.prefab` 当前工作区已存在改动，执行时只能在现状上叠加，不回退既有变化。
- `DrawMapSystem` 的层语义如果与 prefab 子节点顺序不一致，会导致地表显示错层。
- 纯编译通过不等于视觉正确，如需要可追加 Play Mode 截图验证。

## 执行任务

- [x] 收敛 `MapGraph` / `GenMapSystem` 的纯地形生成入口，确保随机岛屿与 biome 生成逻辑一致
- [x] 收敛 `DrawMapSystem` 的 biome 分层判定，确保只输出纯地形层
- [x] 同步 `DrawCarpet` 配置与 `Map.prefab` 图层结构，保证层数、顺序、贴图匹配
- [x] 运行 Unity 编译校验并检查任务范围内 diff
- [x] 提交本次纯地形 MVP 改动
