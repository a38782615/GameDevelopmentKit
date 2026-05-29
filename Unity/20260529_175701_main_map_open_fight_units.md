# Main Map Opens Fight Units

## Summary

- Wired the `UIMain` map button to open `UIFormFight`.
- Added `UIFormFight` runtime state for temporary fight unit ids and `UIHeadItem` views.
- Loaded the first hero and first monster as fight units when `UIFormFight` opens.
- Loaded `UIHeadItem` widgets as the unit views and attached them to `UIFormFight` slots `L0` and `R0`.
- Bound each unit `GameObjectComponent` and ability owner object to its `UIHeadItem` view.

## Files

- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIMain/UIFormMainSystem.cs`
- `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIFight/UIFormFightSystem.cs`
- `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UIFight/UIFormFight.cs`

## Verification

- `AIBridgeCLI.exe compile unity --raw --timeout 120000` succeeded with `errorCount: 0`.
