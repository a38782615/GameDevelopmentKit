# Orthographic Camera Design Resolution Update

## Changes

- Updated `Assets/Scripts/Game/Camera/OrthographicCameraSizeAdapter.cs`.
- Changed default design resolution to `750 x 1335`.
- Kept default design orthographic size as `5`.
- With actual resolution `750 x 1335`, the computed ratio is `1`, so camera `orthographicSize` remains `5`.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
