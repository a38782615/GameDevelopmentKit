# Login Add ArchiveMgr

- Added a LoginFinish event handler that adds ArchiveMgrComponent to the client scene after login.
- Guarded the add operation to avoid duplicate ArchiveMgrComponent instances if LoginFinish is published more than once.
- Verification target: Unity compile through AIBridge.
