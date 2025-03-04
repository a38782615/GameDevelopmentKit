using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public class AI_Attack: AAIHandler
    {
        public override int Check(AIComponent aiComponent, DRAIConfig aiConfig)
        {
            long sec = TimeInfo.Instance.ClientNow() / 1000 % 15;
            if (sec >= 10)
            {
                return 0;
            }
            return 1;
        }

        public override async UniTask Execute(AIComponent aiComponent, DRAIConfig aiConfig, CancellationToken token)
        {
            Fiber fiber = aiComponent.Fiber();

            Unit myUnit = UnitHelper.GetMyUnitFromClientScene(fiber.Root);
            if (myUnit == null)
            {
                return;
            }

            // 停在当前位置
            fiber.Root.GetComponent<ClientSenderComponent>().Send(new C2M_Stop());
            
            Log.Debug("开始攻击");

            for (int i = 0; i < 100000; ++i)
            {
                Log.Debug($"攻击: {i}次");

                // 因为协程可能被中断，任何协程都要传入cancellationToken，判断如果是中断则要返回
                await fiber.Root.GetComponent<TimerComponent>().WaitAsync(1000, token);
            }
        }
    }
}