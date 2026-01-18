---
description: 并行解决代码中的 TODO 注释
argument-hint: 文件或目录路径
---

# /resolve-parallel

并行解决代码文件中的 TODO 注释。

## 用法

```
/resolve-parallel [文件或目录路径]
```

## 目的

通过启动并行代理来查找和解决代码中的 TODO、FIXME 和 HACK 注释。

## 流程

1. **扫描 TODO** - 查找所有 TODO/FIXME/HACK 注释
2. **分类** - 按类型和优先级分组
3. **启动并行代理** - 每个 TODO 一个代理
4. **实现修复** - 每个代理解决其分配的 TODO
5. **验证** - 运行测试确保修复有效

## TODO 类型

- `TODO` - 待实现的功能
- `FIXME` - 待修复的 Bug
- `HACK` - 待处理的技术债务
- `XXX` - 需要关注
- `OPTIMIZE` - 需要性能优化

## 输出

已解决 TODO 的汇总，包含：
- 修改的文件
- 已解决的 TODO
- 任何剩余问题
