## Changes

- Rewrote `AttrCmp.BaseValueInt` with the same getter structure as `BaseValue`.
- Rewrote `AttrCmp.CurrentValueInt` with the same getter structure as `CurrentValue`.
- Both integer views now read through `NumericComponent.GetAsInt(...)` directly.

## Verification

- Ran `AIBridgeCLI.exe focus --raw`
- Ran `AIBridgeCLI.exe editor stop --raw`
- Ran `AIBridgeCLI.exe compile unity --raw --timeout 120000`
- Compile result: `success: true`, `errorCount: 0`
