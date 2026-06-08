using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public class GameAI_Idle : AGameAIHandler
    {
        private const int IdlePollIntervalMs = 50;

        public override int Check(GameAIComponent aiComponent, DRGameAI aiConfig)
        {
            Unit unit = aiComponent?.GetOwnerUnit();
            if (unit == null)
            {
                return 1;
            }

            return aiComponent.HasPendingPatrolIdle() ? 0 : 1;
        }

        public override async UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token)
        {
            TimerComponent timerComponent = aiComponent.Root().GetComponent<TimerComponent>();
            while (!token.IsCancellationRequested)
            {
                int remainingMs = aiComponent.GetRemainingPatrolIdleMs();
                if (remainingMs <= 0)
                {
                    aiComponent.ClearPatrolIdle();
                    return;
                }

                int waitMs = System.Math.Min(IdlePollIntervalMs, remainingMs);
                bool canceled = await timerComponent.WaitAsync(waitMs, token).SuppressCancellationThrow();
                if (canceled)
                {
                    return;
                }

                if (aiComponent.IsAnyAttackInProgress())
                {
                    continue;
                }

                aiComponent.ConsumePatrolIdleMs(waitMs);
            }

            if (token.IsCancellationRequested)
            {
                return;
            }
        }
    }
}
