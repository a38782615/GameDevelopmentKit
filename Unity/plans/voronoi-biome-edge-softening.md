# Voronoi Biome 地形边缘软化

## 问题陈述

当前 `53f54f1c` 之后的纯地形实现按主 `biome` 做 5 层硬切，导致海岸、湖岸、植被边界和寒冷带边缘过于生硬。需要参考该提交之前的 `DrawMapSystem` 过渡逻辑，把 `MapNode.SecondaryCenter`、`EdgeBlend`、`CornerBlend` 重新接回现有 5 层链路。

## 验收标准

- [x] `DrawMapSystem` 的海洋/水域/植被/寒冷边缘重新具备过渡带，而不是只按主 biome 硬切
- [x] `Ground` 层位于基础地表顺序，软化层不会被 `Ground` 覆盖
- [x] Unity 编译通过，且仅引入本次边缘软化相关改动

## 涉及文件

- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/nmap/Tools/DrawMapSystem.cs`
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/nmap/Tools/DrawCarpetSystem.cs`

## 执行任务

- [x] 参考旧版实现恢复边界混合逻辑，并适配现有 5 层 biome 渲染
- [x] 调整 `DrawCarpet` 图层渲染顺序，保证基础层和覆盖层关系正确
- [x] 运行 Unity 编译校验并检查本次 diff
