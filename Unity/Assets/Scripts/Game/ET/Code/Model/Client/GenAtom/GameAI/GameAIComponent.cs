using System.Threading;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class GameAIComponent : Entity, IAwake<int>, IDestroy
    {
        public int AIConfigId;

        public CancellationTokenSource CancellationTokenSource;

        public long Timer;

        public int Current;
    }
}
