---
description: 执行计划文件中的任务
argument-hint: 计划文件路径（可选）
---

# /workflows-work

高效执行工作计划，保持质量并完成功能。

## Execution Workflow

### Phase 1: Quick Start
1. Read plan and clarify any ambiguities
2. Setup environment (branch or worktree)
3. Create todo list from plan tasks

### Phase 2: Execute
1. Task execution loop - mark progress as you go
2. Follow existing patterns in codebase
3. Test continuously after each change
4. Track progress with TodoWrite

### Phase 3: Quality Check
1. Run full test suite
2. Run linting
3. Optional: Use reviewer agents for complex changes

### Phase 4: Ship It
1. Create commit with conventional format
2. Create pull request with summary
3. Notify completion

## Key Principles

- **Start Fast, Execute Faster** - Get clarification once, then execute
- **The Plan is Your Guide** - Follow referenced patterns
- **Test As You Go** - Don't wait until the end
- **Ship Complete Features** - Don't leave features 80% done

## Quality Checklist

- [ ] All tasks marked completed
- [ ] Tests pass
- [ ] Linting passes
- [ ] Code follows existing patterns
- [ ] Commit messages follow conventional format

---

**计划文件**: $ARGUMENTS
