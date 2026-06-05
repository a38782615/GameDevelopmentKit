# Skill HUD Unlit Quad Fix

## Context

- Hit and HUD update logs showed `AttributeChanged`, `VisibleWindow`, and `DrawUnit` were already triggered.
- Runtime HUD objects existed, but `Game/HUD/TextBillboard` quads did not show in GameView.
- A default Unity Quad placed at the same camera-space position was visible, proving camera, layer, position, and coverage were not the root cause.

## Changes

- Switched runtime blood bar quad materials to `Universal Render Pipeline/Unlit`, with `Sprites/Default` and `Game/HUD/TextBillboard` fallback.
- Configured runtime HUD materials as transparent, with alpha blending, `ZWrite` off, and `Cull` off.
- Converted health bars from transient `Graphics.DrawMesh` batches to persistent `MeshRenderer` quad objects for easier runtime inspection.
- Removed hidden runtime flags from HUD render objects so AIBridge can find and inspect them during Play Mode.
- Added throttled `SkillDiagFileLogger` render-state logs for active blood bars.
- Kept the debug preview code available but disabled by default after visual verification.

## Verification

- Unity compile passed through AIBridge with `errorCount=0`.
- GameView screenshot verified the same runtime quad became visible after switching to URP Unlit.
- Chinese encoding check found no Chinese text or replacement-character corruption in the changed files.
