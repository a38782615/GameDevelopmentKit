# ArchiveMgrComponent

- Added `ArchiveMgrComponent` in ET Model to hold current archive state:
  - `CurrentArchiveName`
  - `CurrentArchivePath`
  - `CurrentArchive`
- Added `ArchiveMgrComponentSystem` in ET Hotfix for archive management:
  - `LoadDefaultArchive`
  - `LoadArchive`
  - `ResetDefaultArchive`
  - `ResetArchive`
  - `GetCurrentArchive`
  - `GetDefaultArchiveName`
  - `GetArchivePath`
- Default archive name is `Save` + device id. Because `Game.ET.Code.Model` and `Game.ET.Code.Hotfix` use `noEngineReferences=true`, the device id uses `.NET Environment.MachineName` instead of `UnityEngine.SystemInfo.deviceUniqueIdentifier`.
- Archive file path is `<LocalApplicationData>/myGameDevelopmentKit/Archive/<ArchiveName>.db`.
- Reset closes the current archive if it matches the target name, deletes matching database files, and reopens a fresh archive.
- Validation: `AIBridgeCLI compile unity --raw --timeout 120000` passed with 0 errors and 0 warnings.
