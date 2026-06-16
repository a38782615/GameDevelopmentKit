## Changes

- Switched `AttrCmp.BaseValueInt` and `AttrCmp.CurrentValueInt` to read numeric slots through `NumericComponent.GetAsInt(...)`.
- Kept truncation semantics. No rounding is introduced.
- Because `AttrCmp` values are stored as fixed-point `float * 10000`, the raw `GetAsInt(...)` result is truncated with `/ 10000` before exposing the integer view.

## Effect

- Integer views now share the `NumericComponent` read path instead of converting from `float`.
- `AttackSpeed = 1.9` still produces `1`, not `2`.

## Verification

- Ran `AIBridgeCLI.exe focus --raw`
- Ran `AIBridgeCLI.exe editor stop --raw`
- Ran `AIBridgeCLI.exe compile unity --raw --timeout 120000`
- Compile result: `success: true`, `errorCount: 0`
