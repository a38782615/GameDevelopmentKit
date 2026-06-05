# Skill AI Dead Unit Guard

## Context

- Enemy health bars could reach zero while the enemy still continued attacking.
- Existing AI target lookup filtered dead targets, but the caster itself was not consistently checked before activating skills.

## Changes

- Added `AbilitySystemComponent.IsAlive()` as the shared HP-alive check.
- Blocked `TryActivateAbility` when the caster is dead.
- Blocked activation against a dead explicit or resolved main target.
- On HP transition from positive to zero or below:
  - logged a `[Death]` diagnostic line through `SkillDiagFileLogger`
  - dispatched `OnDeath`
  - cancelled active abilities on the dead ASC
- Added alive checks to `GameAI_Attack` check and execute paths.
- Updated AI target utility and `SearchTargetTaskSpecHandler` to use the shared alive check.
- Rewrote the touched ASC system file to remove pre-existing garbled Chinese comments in that file.

## Verification

- Unity compile passed through AIBridge with `errorCount=0`.
- Play Mode log showed enemies reaching zero HP, `[Death]` logs being emitted, and their `7001` auto skill ending with `cancelled=True`.
- After the first enemy died, subsequent attacks selected the next living enemy instead of continuing to attack the zero-HP unit.
- Unity Console Error log count was `0`.
- Chinese encoding check found no Chinese text or replacement-character corruption in the changed files.
