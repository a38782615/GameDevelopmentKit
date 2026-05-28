# Recent Prefab Tool

## Summary

- Added `Game/Tool/Recent Prefabs` editor window.
- Added recent prefab history tracking for prefab open, prefab mode open, and project selection.
- Added `Show`, `Open`, and `Locate` actions for each recent prefab entry.
- Stored recent history in `EditorPrefs` and cleaned invalid prefab records automatically.

## Files

- `Assets/Scripts/Game/Editor/Tool/RecentPrefabAccessService.cs`
- `Assets/Scripts/Game/Editor/Tool/RecentPrefabWindow.cs`

## Verification

- `AIBridgeCLI.exe compile unity --raw --timeout 120000` succeeded with `errorCount: 0`.
- `AIBridgeCLI.exe menu_item --menuPath "Game/Tool/Recent Prefabs" --raw` succeeded with `executed: true`.
