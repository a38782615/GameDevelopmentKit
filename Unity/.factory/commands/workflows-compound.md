---
description: 记录解决的问题，积累团队知识
argument-hint: 问题描述（可选）
---

# /workflows-compound

记录最近解决的问题，积累团队知识。

## Purpose

Captures problem solutions while context is fresh, creating structured documentation for future reference.

**Why "compound"?** Each documented solution compounds your team's knowledge. The first time you solve a problem takes research. Document it, and the next occurrence takes minutes.

## What It Captures

- **Problem symptom**: Exact error messages, observable behavior
- **Investigation steps tried**: What didn't work and why
- **Root cause analysis**: Technical explanation
- **Working solution**: Step-by-step fix with code examples
- **Prevention strategies**: How to avoid in future
- **Cross-references**: Links to related issues and docs

## Output Categories

- build-errors/
- test-failures/
- runtime-errors/
- performance-issues/
- database-issues/
- security-issues/
- ui-bugs/
- integration-issues/
- logic-errors/

## Execution Rules (MANDATORY)

执行此命令时，**必须**使用 Task 工具启动子代理来完成文档：

### Step 1: 启动子代理（并行）

使用 Task 工具启动 `technical-writer` 子代理，提供以下信息：

```
在 docs/compound/{category}/ 目录下创建问题文档。

问题描述: {从对话历史提取}

需要包含：
1. 问题症状 - 具体错误信息或行为描述
2. 根因分析 - 技术层面的原因解释  
3. 解决方案 - 代码示例
4. 涉及文件 - 文件列表
5. 预防策略 - 如何避免类似问题
6. 标签 - 相关标签

分类选择: {根据问题类型选择 Output Categories}
```

### Step 2: 确认完成

子代理完成后，更新 TODO 列表标记文档已创建。

## The Compounding Philosophy

```
Plan -> Work -> Review -> Compound -> Repeat
```

Each cycle compounds: plans inform future plans, reviews catch more issues, patterns get documented.

**Each unit of engineering work should make subsequent units easier—not harder.**

---

**问题描述**: $ARGUMENTS
