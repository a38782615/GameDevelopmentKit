# Orthographic Camera Execute Always

## Changes

- Updated `Assets/Scripts/Game/Camera/OrthographicCameraSizeAdapter.cs`.
- Added `ExecuteAlways` so the component also runs in edit mode.
- Resolution changes in the editor Game view now trigger `Update`, which reapplies camera size when `Screen.width` or `Screen.height` changes.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
