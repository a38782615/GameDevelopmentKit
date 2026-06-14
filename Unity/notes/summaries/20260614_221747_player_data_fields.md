# PlayerData fields

- Added new `PlayerData` integer fields:
  - `ElixirPoison`
  - `Physique`
  - `Comprehension`
  - `DivineSense`
  - `Fortune`
- Kept the current workspace rename from `ArchiveData.cs` to `PlayerData.cs` because Unity regenerated `Game.ET.Code.Model.csproj` to compile `PlayerData.cs`.
- Preserved the Unity meta GUID on `PlayerData.cs.meta`.
- Verified `PlayerData.cs` has no UTF-8 replacement markers or question-mark comment replacement.
- Validation: `AIBridgeCLI compile unity --raw --timeout 120000` passed with 0 errors and 0 warnings.
