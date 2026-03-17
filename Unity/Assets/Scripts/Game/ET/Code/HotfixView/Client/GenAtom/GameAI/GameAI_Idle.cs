using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public class GameAI_Idle : AGameAIHandler
    {
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
            int remainingMs = aiComponent.GetRemainingPatrolIdleMs();
            if (remainingMs <= 0)
            {
                aiComponent.ClearPatrolIdle();
                return;
            }

            bool canceled = await aiComponent.Root().GetComponent<TimerComponent>().WaitAsync(remainingMs, token).SuppressCancellationThrow();
            if (canceled)
            {
                return;
            }

            aiComponent.ClearPatrolIdle();
        }
    }
}
