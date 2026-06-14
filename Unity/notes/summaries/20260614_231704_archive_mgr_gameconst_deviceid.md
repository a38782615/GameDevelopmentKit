# ArchiveMgr GameConst DeviceId

- Updated ArchiveMgrComponentSystem default archive device id lookup to prefer GameConst.DeviceId.
- Kept the existing environment fallback path so empty GameConst.DeviceId does not produce an invalid default archive name.
- Verification target: Unity compile through AIBridge.
