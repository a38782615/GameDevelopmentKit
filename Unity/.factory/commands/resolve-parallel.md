---
description: 并行解决代码中的 TODO 注释
argument-hint: 文件或目录路径
---

# /resolve-parallel

Resolve TODO comments in code files in parallel.

## Usage

```
/resolve-parallel [file or directory]
```

## Purpose

Find and resolve TODO, FIXME, and HACK comments in code by launching parallel agents to address each one.

## Process

1. **Scan for TODOs** - Find all TODO/FIXME/HACK comments
2. **Categorize** - Group by type and priority
3. **Launch Parallel Agents** - One agent per TODO
4. **Implement Fixes** - Each agent resolves its assigned TODO
5. **Verify** - Run tests to ensure fixes work

## TODO Types

- `TODO` - Feature to implement
- `FIXME` - Bug to fix
- `HACK` - Technical debt to address
- `XXX` - Needs attention
- `OPTIMIZE` - Performance improvement needed

## Output

Summary of resolved TODOs with:
- Files modified
- TODOs resolved
- Any remaining issues
