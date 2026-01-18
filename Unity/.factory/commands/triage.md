---
description: 对问题、bug 或任务进行分类和优先级排序
argument-hint: 问题列表或目录
---

# /triage

Triage and prioritize issues, bugs, or tasks.

## Usage

```
/triage [issue list or directory]
```

## Purpose

Systematically evaluate and prioritize a list of issues, bugs, or tasks based on impact, effort, and urgency.

## Triage Criteria

### Severity Levels
- **P1 CRITICAL** - System down, data loss, security breach
- **P2 HIGH** - Major feature broken, significant user impact
- **P3 MEDIUM** - Feature degraded, workaround exists
- **P4 LOW** - Minor issue, cosmetic, nice-to-have

### Effort Estimation
- **XS** - < 1 hour
- **S** - 1-4 hours
- **M** - 1-2 days
- **L** - 3-5 days
- **XL** - > 1 week

### Priority Matrix

| Impact \ Effort | XS | S | M | L | XL |
|-----------------|----|----|----|----|-----|
| Critical | NOW | NOW | NOW | Plan | Plan |
| High | NOW | Soon | Soon | Plan | Backlog |
| Medium | Soon | Soon | Plan | Backlog | Backlog |
| Low | Soon | Plan | Backlog | Backlog | Backlog |

## Output

Prioritized list with:
- Severity assignment
- Effort estimate
- Recommended action (NOW/Soon/Plan/Backlog)
- Dependencies identified
