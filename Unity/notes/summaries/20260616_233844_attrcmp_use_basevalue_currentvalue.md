## Changes

- Simplified `AttrCmp.BaseValueInt` to `=> (int)BaseValue`.
- Simplified `AttrCmp.CurrentValueInt` to `=> (int)CurrentValue`.
- Removed the extra `TruncateRawNumericToInt` helper.

## Effect

- Integer views now directly follow the same semantic path as `BaseValue` and `CurrentValue`.
- The cast still truncates toward zero and does not round.

## Verification

- Ran `AIBridgeCLI.exe focus --raw`
- Ran `AIBridgeCLI.exe editor stop --raw`
- Ran `AIBridgeCLI.exe compile unity --raw --timeout 120000`
- Compile result: `success: true`, `errorCount: 0`
