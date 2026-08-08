<!-- AIBRIDGE:START {"assistant":"claude","templateId":"unity-integration","version":1,"target":"root-rule"} -->
## AIBridge Unity Integration

Use `AIBridgeCache/CLI/AIBridgeCLI.exe` to interact with Unity Editor through AIBridge.

**Skill**: `aibridge`

**When to Use**:
- Read Unity console errors and warnings
- Trigger compile checks and inspect results
- Create or modify GameObjects, Components, Scenes, and Prefabs
- Search assets and capture screenshots or GIFs from Play Mode

**Quick Reference**:
```bash
# CLI Path
AIBridgeCache/CLI/AIBridgeCLI.exe

# Common Commands
AIBridgeCLI.exe compile unity --raw
AIBridgeCLI.exe get_logs --logType Error --raw
AIBridgeCLI.exe asset search --mode script --keyword "Player" --raw
AIBridgeCLI.exe gameobject create --name "Cube" --primitiveType Cube --raw
AIBridgeCLI.exe transform set_position --path "Player" --x 0 --y 1 --z 0 --raw
```

**Skill Documentation**: [AIBridge Skill](/.claude/skills/aibridge/SKILL.md)
<!-- AIBRIDGE:END -->
