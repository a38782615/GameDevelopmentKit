# UIBtmBar extract logic

## Changes

- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIMain/UIFormMainSystem.cs`.
- `UIFormMain` now only calls `OpenAllUIWidgets()` on open.
- Updated `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UIBtmBar/UIWidgetBtmBar.cs`.
- Added `ComponentOf(typeof(UIFormMain))` to make the bottom bar widget a child widget of `UIFormMain`.
- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIBtmBar/UIWidgetBtmBarSystem.cs`.
- Moved button bind, unbind, and click handling logic from `UIFormMainSystem` into `UIWidgetBtmBarSystem`.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
