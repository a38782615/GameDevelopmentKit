## Changes

- Added `NumericType.MaxAge = 1028`.
- Added `NumericType.MaxAgeBase`.
- Registered `MaxAge` in client attribute types, base numeric mapping, display name mapping, and name parsing.

## Verification

- Ran `AIBridgeCLI.exe focus --raw`
- Ran `AIBridgeCLI.exe editor stop --raw`
- Ran `AIBridgeCLI.exe compile unity --raw --timeout 120000`
- Compile result: `success: true`, `errorCount: 0`
