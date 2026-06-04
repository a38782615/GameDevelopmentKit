# UIMain bottom bar widget bind fix

## Changes

- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIMain/UIFormMainSystem.cs`.
- Switched button binding and unbinding to `self.View.BtmBarBtmBar`.
- Kept the existing `actionName` handling logic unchanged.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
