# Orthographic Camera Size Adapter

## Changes

- Added `Assets/Scripts/Game/Camera/OrthographicCameraSizeAdapter.cs`.
- Exposed configurable design resolution via `Vector2Int`.
- Exposed configurable base orthographic size.
- Applied runtime scaling only when actual screen resolution is smaller than design resolution.
- Kept the base size unchanged when actual resolution is greater than or equal to design resolution.
- Reapplied camera size automatically when `Screen.width` or `Screen.height` changes.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
