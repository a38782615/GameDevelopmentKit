# ArchiveMgr Remove Environment

- Removed Environment fallback from ArchiveMgrComponentSystem device id lookup.
- GameConst.DeviceId is now required for the default archive name.
- Changed archive directory lookup to use AppContext.BaseDirectory instead of Environment.GetFolderPath.
- Verification target: Unity compile through AIBridge.
