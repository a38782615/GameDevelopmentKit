## Changes

- Added setters for `AttrCmp.BaseValueInt` and `AttrCmp.CurrentValueInt`.
- Added integer overloads for `WriteBaseValue(...)` and `WriteCurrentValue(...)`.
- Integer setters now write through `NumericComponent.Set(..., int)` to match the existing `GetAsInt(...)` getter path.

## Verification

- Ran `AIBridgeCLI.exe focus --raw`
- Ran `AIBridgeCLI.exe editor stop --raw`
- Ran `AIBridgeCLI.exe compile unity --raw --timeout 120000`
- Compile result: `success: true`, `errorCount: 0`
