# ArchiveMgr Remove Normalize

- Removed NormalizeArchiveName usage from ArchiveMgrComponentSystem.
- Archive names now pass through directly after empty/whitespace validation.
- GameConst.DeviceId is returned directly after empty/whitespace validation.
- Verification target: Unity compile through AIBridge.
