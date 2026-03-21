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
                return 1;
            }

            GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
            if (spec == null)
            {
                return 1;
            }

            if (asc.IsCasting(spec))
            {
                return 1;
            }

            if (!spec.CanActivate())
            {
                return 1;
            }

            float attackRange = aiConfig.GetAttackRange();
            AbilitySystemComponent target = aiComponent.FindNearestTarget(attackRange);
            return target != null ? 0 : 1;
        }

        public override async UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                AbilitySystemComponent asc = aiComponent?.GetOwnerASC();
                if (asc?.Abilities == null)
                {
                    return;
                }

                GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
                if (spec == null)
                {
                    return;
                }

                AbilitySystemComponent target = aiComponent.FindNearestTarget(aiConfig.GetAttackRange());
                if (target == null)
                {
                    return;
                }

                asc.TryActivateAbility(spec, target);
                bool canceled = await aiComponent.Root().GetComponent<TimerComponent>().WaitAsync(500, token).SuppressCancellationThrow();
                if (canceled)
                {
                    return;
                }
            }
        }
    }
}
