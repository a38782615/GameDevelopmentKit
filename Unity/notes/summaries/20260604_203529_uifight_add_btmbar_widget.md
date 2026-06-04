# UIFight add bottom bar widget

## Changes

- Updated `Assets/Res/UI/UIForm/GenAtom/UIFormFight.prefab`.
- Added `BtmBar_BtmBar` to `UIFormFight` using the shared `BtmBar` UIEntity prefab.
- Generated `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UIFight/MonoUIFormFight.Bind.cs`.
- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIFight/UIFormFightSystem.cs`.
- `UIFormFight` now calls `OpenAllUIWidgets()` on open.
- Updated `Assets/Scripts/Game/ET/Code/ModelView/Client/GenAtom/UI/UIBtmBar/UIWidgetBtmBar.cs`.
- Made `UIWidgetBtmBar` reusable across different `UGFUIForm` parents.
- Updated `Assets/Scripts/Game/ET/Code/HotfixView/Client/GenAtom/UI/UIBtmBar/UIWidgetBtmBarSystem.cs`.
- Switched bottom bar owner lookup from `UIFormMain` to `UGFUIForm`.
- Added `Assets/Scripts/Game/ET/Editor/GenAtom/UIFightPrefabBuilder.cs` to finalize the fight prefab and refresh CodeBind serialization.

## Verification

- Ran Unity compile through `AIBridgeCLI`.
- Compile result: success.
- Error count: `0`.
- Warning count: `0`.
