---
description: 从 git 历史生成变更日志
argument-hint: 起始日期或 commit hash
---

# /changelog

Create engaging changelogs for recent merges.

## Usage

```
/changelog [起始日期或 commit]
```

## 目的

从最近的 git 历史生成格式良好的变更日志，适用于发布说明或团队更新。

## 流程

1. **收集提交** - 读取指定时间点以来的 git 日志
2. **分类变更** - 按类型分组（feat、fix、refactor 等）
3. **提取亮点** - 识别值得注意的变更
4. **格式化输出** - 创建可读的变更日志

## 变更日志格式

```markdown
# 变更日志

## [Version] - YYYY-MM-DD

### 新增
- 新功能描述

### 变更
- 变更描述

### 修复
- Bug 修复描述

### 移除
- 移除的功能描述
```

## 提交类型映射

| 前缀 | 分类 |
|--------|----------|
| feat | 新增 |
| fix | 修复 |
| refactor | 变更 |
| docs | 文档 |
| test | 测试 |
| chore | 维护 |
