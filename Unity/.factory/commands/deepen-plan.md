---
description: 使用并行研究代理深化计划细节
argument-hint: 计划文件路径
---

# /deepen-plan

Enhance plans with parallel research agents for each section.

## Usage

```
/deepen-plan [plan file path]
```

## Purpose

Takes an existing plan and enriches each section with deeper research, best practices, and implementation details using parallel agents.

## Process

1. **Parse Plan** - Read the plan file and identify sections
2. **Launch Parallel Research** - For each section:
   - `best-practices-researcher` - Industry standards
   - `repo-research-analyst` - Existing patterns in codebase
   - `performance-oracle` - Performance considerations
3. **Synthesize Results** - Merge findings into enhanced plan
4. **Update Plan File** - Write enriched version

## When to Use

- After `/workflows:plan` for complex features
- When you need more implementation details
- Before starting work on unfamiliar areas
- When the plan feels too high-level

## Output

Enhanced plan file with:
- Deeper technical details
- Code examples from codebase
- Best practices references
- Performance considerations
