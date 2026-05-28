# Recent Prefab Window Simplify

## Summary

- Removed detailed fields from each recent prefab entry.
- Kept only the prefab object row and the `Show`, `Open`, `Locate` actions.

## Files

- `Assets/Scripts/Game/Editor/Tool/RecentPrefabWindow.cs`

## Verification

- `AIBridgeCLI.exe compile unity --raw --timeout 120000` succeeded with `errorCount: 0`.
