

## AIBridge Unity Integration

**Skill**: `aibridge`

**Activation Keywords**: Unity log, compile Unity, modify asset, query asset, GameObject, Transform, Component, Scene, Prefab, screenshot, GIF

**When to Activate**:
- Get Unity console logs or compilation errors
- Compile Unity project and check results
- Create/modify/delete GameObjects in scene
- Manipulate Transform (position/rotation/scale)
- Add/remove/modify Components
- Load/save scenes, query scene hierarchy
- Instantiate or modify Prefabs
- Search assets in AssetDatabase
- Capture screenshots or record GIFs (Play Mode)

**Quick Reference**:
```bash
# CLI Path
AIBridgeCache/CLI/AIBridgeCLI.exe

# Common Commands
AIBridgeCLI.exe compile unity --raw          # Compile and get errors
AIBridgeCLI.exe get_logs --logType Error     # Get error logs
AIBridgeCLI.exe asset search --mode script --keyword "Player"  # Search scripts
AIBridgeCLI.exe gameobject create --name "Cube" --primitiveType Cube
AIBridgeCLI.exe transform set_position --path "Player" --x 0 --y 1 --z 0
```

**Skill Documentation**: [AIBridge Skill](/.claude/skills/aibridge/SKILL.md)
