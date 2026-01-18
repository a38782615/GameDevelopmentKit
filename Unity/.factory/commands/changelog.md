---
description: 从 git 历史生成变更日志
argument-hint: 起始日期或 commit hash
---

# /changelog

Create engaging changelogs for recent merges.

## Usage

```
/changelog [since date or commit]
```

## Purpose

Generate a well-formatted changelog from recent git history, suitable for release notes or team updates.

## Process

1. **Gather Commits** - Read git log since specified point
2. **Categorize Changes** - Group by type (feat, fix, refactor, etc.)
3. **Extract Highlights** - Identify notable changes
4. **Format Output** - Create readable changelog

## Changelog Format

```markdown
# Changelog

## [Version] - YYYY-MM-DD

### Added
- New feature description

### Changed
- Change description

### Fixed
- Bug fix description

### Removed
- Removed feature description
```

## Commit Type Mapping

| Prefix | Category |
|--------|----------|
| feat | Added |
| fix | Fixed |
| refactor | Changed |
| docs | Documentation |
| test | Testing |
| chore | Maintenance |
