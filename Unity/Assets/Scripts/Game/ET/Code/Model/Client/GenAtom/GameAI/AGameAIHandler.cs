using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    public class GameAIHandlerAttribute : BaseAttribute
    {
    }

    [GameAIHandler]
    public abstract class AGameAIHandler : HandlerObject
    {
        public abstract int Check(GameAIComponent aiComponent, DRGameAI aiConfig);

        public abstract UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token);
    }
}
