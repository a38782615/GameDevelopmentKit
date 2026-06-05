# Skill HUD Single Mesh Batch

## Context

- The previous health bar implementation was visible but used two `MeshRenderer` objects and separate runtime materials per visible unit.
- The user requested a single mesh batch path to reduce renderer count and draw overhead.

## Changes

- Replaced per-unit background/foreground `GameObject + MeshRenderer` health bars with one `SkillHudBloodBarBatch` runtime object.
- Added one dynamic `SkillHudBloodBarBatchMesh` rebuilt each HUD tick from visible health bar quads.
- Split the batch mesh into three submeshes:
  - background bars
  - player foreground bars
  - monster foreground bars
- Reused URP Unlit runtime materials, so draw calls are bounded by the three submeshes rather than by unit count.
- Disabled verbose per-frame HUD draw logs by default through `VerboseHudLog = false` to avoid log IO cost.
- Kept important registration and attribute-change logs through `SkillDiagFileLogger`.

## Verification

- Unity compile passed through AIBridge with `errorCount=0`.
- Play Mode created `SkillHudBloodBarBatch` with one `MeshFilter` and one `MeshRenderer`.
- Unity Console Error log count was `0`.
- A Play Mode run reached actual damage and `VisibleWindow` logs using the new batch implementation.
- Chinese encoding check found no Chinese text or replacement-character corruption in the changed runtime file.
