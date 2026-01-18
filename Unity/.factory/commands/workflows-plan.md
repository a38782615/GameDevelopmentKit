---
description: 将功能描述转换为结构化的计划文档
argument-hint: 功能描述
---

# /workflows-plan

将功能描述、bug报告或改进想法转换为结构化的 markdown 计划文档。

## Workflow

1. **Repository Research** - Understand project conventions and patterns
2. **Issue Planning** - Draft clear, actionable plan structure
3. **Choose Detail Level** - **必须询问用户**选择详细程度，等待回复后再继续
4. **Create Plan File** - Write to `plans/<issue_title>.md`

## 执行规则（必须遵守）

**在创建计划文件之前，必须先询问用户选择详细程度：**

请选择计划的详细程度：
- **MINIMAL** - 快速计划
- **MORE** - 标准计划
- **A LOT** - 详尽计划

**等待用户回复后，再根据选择的级别创建计划文档。不得跳过此步骤。**

## Detail Levels

### MINIMAL (Quick Issue)
- Problem statement
- Basic acceptance criteria
- Essential context only

### MORE (Standard Issue)
- Detailed background and motivation
- Technical considerations
- Success metrics
- Dependencies and risks

### A LOT (Comprehensive Issue)
- Detailed implementation phases
- Alternative approaches considered
- Resource requirements and timeline
- Risk mitigation strategies

## Output

Plan file saved to `plans/<issue_title>.md`

## Next Steps After Planning

1. Run `/workflows-work` to execute the plan
2. Run `/workflows-review` after implementation
3. Run `/workflows-compound` to document learnings

---

**功能描述**: $ARGUMENTS
