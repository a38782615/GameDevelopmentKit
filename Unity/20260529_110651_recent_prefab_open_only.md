# Recent Prefab Open Only

## Summary

- Simplified `RecentPrefabWindow` actions to keep only the `Open` button.
- Removed `Show` and `Locate` actions from recent prefab entries.

## Files

- `Assets/Scripts/Game/Editor/Tool/RecentPrefabWindow.cs`

## Verification

- `AIBridgeCLI.exe compile unity --raw --timeout 120000` succeeded with `errorCount: 0`.
