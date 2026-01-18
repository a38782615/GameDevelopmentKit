# *MUST*使用中文回答用户问题

---

## Plan Completion Rule

执行 `plans/*.md` 中的任务时，完成每个任务后**必须**用 Edit 工具将对应的 `- [ ]` 改为 `- [x]`，保持计划文件与实际进度同步。

---

## Compound Engineering Workflows

使用 Factory 自定义命令实现工程工作流。命令文件位于 `.factory/commands/` 目录。

**可用命令**：
- `/workflows-plan <功能描述>` - 将功能描述转换为结构化计划文档
- `/workflows-work [计划文件]` - 执行计划文件中的任务
- `/workflows-review [目标]` - 多角度代码审查
- `/workflows-compound [问题描述]` - 记录解决的问题，积累知识

---

**核心理念**：`Plan → Work → Review → Compound → Repeat`

**80/20 原则**：80% 在规划和审查，20% 在执行。
