using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [FriendOf(typeof(GameplayAbilitySpec))]
    [FriendOf(typeof(GameAIComponent))]
    public class GameAI_Attack : AGameAIHandler
    {
        private const int AttackPollIntervalMs = 50;

        public override int Check(GameAIComponent aiComponent, DRGameAI aiConfig)
        {
            AbilitySystemComponent asc = aiComponent?.GetOwnerASC();
            if (asc?.Abilities == null || !asc.IsAlive())
            {
                return 1;
            }

            GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
            if (spec == null)
            {
                return 1;
            }

            if (asc.IsCasting())
            {
                return 1;
            }

            if (!spec.CanActivate())
            {
                return 1;
            }

            float attackRange = aiConfig.GetAttackRange();
            AbilitySystemComponent target = aiComponent.FindNearestTarget(attackRange);
            if (target == null || !target.IsAlive())
            {
                return 1;
            }

            return 0;
        }

        public override async UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                AbilitySystemComponent asc = aiComponent?.GetOwnerASC();
                if (asc?.Abilities == null || !asc.IsAlive())
                {
                    return;
                }

                GameplayAbilitySpec spec = aiComponent.FindPreferredAbility(aiConfig);
                if (spec == null)
                {
                    return;
                }

                AbilitySystemComponent target = aiComponent.FindNearestTarget(aiConfig.GetAttackRange());
                if (target == null || !target.IsAlive())
                {
                    return;
                }
                if (asc.TryActivateAbility(spec, target))
                {
                    bool canceled = await WaitAttackIntervalAsync(aiComponent, asc, token);
                    if (canceled)
                    {
                        return;
                    }
                    continue;
                }

                bool retryCanceled = await aiComponent.Root().GetComponent<TimerComponent>().WaitAsync(AttackPollIntervalMs, token).SuppressCancellationThrow();
                if (retryCanceled)
                {
                    return;
                }
            }

        }

        private static async UniTask<bool> WaitAttackIntervalAsync(GameAIComponent aiComponent, AbilitySystemComponent asc, CancellationToken token)
        {
            int remainingMs = aiComponent.GetAttackIntervalMs();
            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            while (remainingMs > 0 && !token.IsCancellationRequested)
            {
                int waitMs = System.Math.Min(AttackPollIntervalMs, remainingMs);
                bool canceled = await timerComponent.WaitAsync(waitMs, token).SuppressCancellationThrow();
                if (canceled)
                {
                    return true;
                }

                if (aiComponent.IsAnyAttackInProgress())
                {
                    continue;
                }

                remainingMs -= waitMs;
            }

            return token.IsCancellationRequested;
        }
    }
}
