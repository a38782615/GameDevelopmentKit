# UIMain Map Scene Trigger Fix

## Changes

- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIMain/UIFormMainSystem.cs`.
- Kept `actionName` values sourced from `GameConst`.
- Changed the scene-switch branch to trigger on `GameConst.Btm_Map`.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
