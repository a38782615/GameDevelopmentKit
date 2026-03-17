using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [FriendOf(typeof(GameplayAbilitySpec))]
    public class GameAI_Attack : AGameAIHandler
    {
        public override int Check(GameAIComponent aiComponent, DRGameAI aiConfig)
        {
            AbilitySystemComponent asc = aiComponent?.GetOwnerASC();
            if (asc?.Abilities == null)
            {
#if UNITY_EDITOR
                Unit unit = aiComponent?.GetOwnerUnit();
                if (unit != null)
                {
                    SkillDiagFileLogger.Log($"[DiagGameAI] attack check-skip abilities-missing unit={unit.Id} config={unit.ConfigId}");
                }
#endif
                return 1;
            }

            GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
            if (spec == null)
            {
#if UNITY_EDITOR
                Unit unit = aiComponent?.GetOwnerUnit();
                if (unit != null)
                {
                    SkillDiagFileLogger.Log($"[DiagGameAI] attack check-skip spec-missing unit={unit.Id} config={unit.ConfigId}");
                }
#endif
                return 1;
            }

            float attackRange = aiConfig.GetAttackRange();
            AbilitySystemComponent target = aiComponent.FindNearestTarget(attackRange);
#if UNITY_EDITOR
            Unit ownerUnit = aiComponent?.GetOwnerUnit();
            Unit targetUnit = target?.GetParent<SkillUnit>()?.Unit.As();
            if (ownerUnit != null)
            {
                SkillDiagFileLogger.Log(
                    $"[DiagGameAI] attack check unit={ownerUnit.Id} config={ownerUnit.ConfigId} spec={spec.AbilityNodeData?.skillId ?? 0} state={spec.State} active={spec.IsActive} cooldown={spec.IsOnCooldown()} canActivate={spec.CanActivate()} range={attackRange:0.##} target={(targetUnit == null ? 0 : targetUnit.Id)}");
            }
#endif
            return target != null ? 0 : 1;
        }

        public override async UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                AbilitySystemComponent asc = aiComponent?.GetOwnerASC();
                if (asc?.Abilities == null)
                {
#if UNITY_EDITOR
                    Unit unit = aiComponent?.GetOwnerUnit();
                    if (unit != null)
                    {
                        SkillDiagFileLogger.Log($"[DiagGameAI] attack execute-stop abilities-missing unit={unit.Id} config={unit.ConfigId}");
                    }
#endif
                    return;
                }

                GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
                if (spec == null)
                {
#if UNITY_EDITOR
                    Unit unit = aiComponent?.GetOwnerUnit();
                    if (unit != null)
                    {
                        SkillDiagFileLogger.Log($"[DiagGameAI] attack execute-stop spec-missing unit={unit.Id} config={unit.ConfigId}");
                    }
#endif
                    return;
                }

                AbilitySystemComponent target = aiComponent.FindNearestTarget(aiConfig.GetAttackRange());
                if (target == null)
                {
#if UNITY_EDITOR
                    Unit unit = aiComponent?.GetOwnerUnit();
                    if (unit != null)
                    {
                        SkillDiagFileLogger.Log($"[DiagGameAI] attack execute-stop target-missing unit={unit.Id} config={unit.ConfigId}");
                    }
#endif
                    return;
                }

                bool success = asc.TryActivateAbility(spec, target);
#if UNITY_EDITOR
                Unit ownerUnit = aiComponent.GetOwnerUnit();
                Unit targetUnit = target.GetParent<SkillUnit>()?.Unit.As();
                if (ownerUnit != null)
                {
                    SkillDiagFileLogger.Log(
                        $"[DiagGameAI] attack execute unit={ownerUnit.Id} config={ownerUnit.ConfigId} spec={spec.AbilityNodeData?.skillId ?? 0} state={spec.State} active={spec.IsActive} running={spec.IsRunning} cooldown={spec.IsOnCooldown()} canActivate={spec.CanActivate()} success={success} target={(targetUnit == null ? 0 : targetUnit.Id)}");
                }
#endif
                bool canceled = await aiComponent.Root().GetComponent<TimerComponent>().WaitAsync(500, token).SuppressCancellationThrow();
                if (canceled)
                {
                    return;
                }
            }
        }
    }
}
