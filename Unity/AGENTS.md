<!-- AIBRIDGE:START {"assistant":"codex","templateId":"unity-integration","version":2,"target":"root-rule"} -->
## AIBridge Rules

**Skill**: `aibridge` - Unity CLI automation

**CLI**: `./AIBridgeCache/CLI/AIBridgeCLI.exe` (JSON output)

**Priority**:
- **Compile**: `compile unity` (default), `compile dotnet` (optional)
- **Asset Search**: `asset search/find --format paths` before filesystem search
- **Console**: `get_logs --logType Error`

**Quick Reference**:
```bash
./AIBridgeCache/CLI/AIBridgeCLI.exe compile unity
./AIBridgeCache/CLI/AIBridgeCLI.exe get_logs --logType Error
./AIBridgeCache/CLI/AIBridgeCLI.exe asset search --mode script --keyword "Player" --format paths
./AIBridgeCache/CLI/AIBridgeCLI.exe gameobject create --name "Cube" --primitiveType Cube
```

Reference: `/Packages/cn.lys.aibridge/Skill~/SKILL.md`
<!-- AIBRIDGE:END -->

<!-- AIBRIDGE:START {"assistant":"codex","templateId":"unity-project-rules","version":1,"target":"root-rule"} -->
## AIBridge Rules

Use `AIBridgeCache/CLI/AIBridgeCLI.exe` for Unity Editor automation in this project.

- Prefer `--raw` output for machine-readable responses
- Use AIBridge for compile checks, console log inspection, scene hierarchy changes, GameObject updates, Transform edits, and asset queries
- Use screenshot or GIF commands for visual verification when Play Mode is required

**Quick Reference**:
```bash
AIBridgeCLI.exe compile unity --raw
AIBridgeCLI.exe get_logs --logType Error --raw
AIBridgeCLI.exe gameobject create --name "Cube" --primitiveType Cube --raw
AIBridgeCLI.exe asset search --mode script --keyword "Player" --raw
```

Reference: `/.claude/skills/aibridge/SKILL.md`
<!-- AIBRIDGE:END -->
