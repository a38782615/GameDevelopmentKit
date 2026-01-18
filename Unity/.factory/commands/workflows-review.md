---
description: 使用多代理分析进行代码审查
argument-hint: PR号、分支名或文件路径
---

# /workflows-review

使用多代理分析进行详尽的代码审查。

## Review Process

### 1. Setup
- Determine review target type
- Checkout the branch being reviewed
- Fetch PR metadata if applicable

### 2. Parallel Agent Analysis

Launch these agents in parallel:

| Agent | Focus |
|-------|-------|
| `architecture-strategist` | Architectural compliance |
| `code-simplicity-reviewer` | Simplicity and minimalism |
| `security-sentinel` | Security vulnerabilities |
| `performance-oracle` | Performance issues |
| `pattern-recognition-specialist` | Patterns and anti-patterns |
| `git-history-analyzer` | Code evolution context |

### 3. Stakeholder Perspectives

Consider viewpoints of:
- **Developer**: Ease of understanding and modification
- **Operations**: Deployment and monitoring
- **End User**: Intuitiveness and performance
- **Security Team**: Attack surface and compliance
- **Business**: ROI and risks

### 4. Scenario Exploration

- Happy path
- Invalid inputs
- Boundary conditions
- Concurrent access
- Scale testing
- Network issues
- Security attacks

### 5. Findings Synthesis

Categorize findings by severity:
- **P1 CRITICAL** - Blocks merge
- **P2 IMPORTANT** - Should fix
- **P3 NICE-TO-HAVE** - Enhancements

## Output

Summary report with:
- Total findings count by severity
- Created todo files for each finding
- Recommended next steps

---

**审查目标**: $ARGUMENTS
