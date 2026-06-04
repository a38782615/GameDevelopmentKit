# UIFormMain actionName use GameConst

## Changes

- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIMain/UIFormMainSystem.cs`.
- Replaced hard-coded `actionName` strings in button binding with `GameConst` values.
- Replaced the `"Bag"` click comparison with `GameConst.Btm_Bag`.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
