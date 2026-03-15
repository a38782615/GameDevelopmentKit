using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [EntitySystemOf(typeof(GameAIComponent))]
    [FriendOf(typeof(GameAIComponent))]
    [FriendOf(typeof(GameAIDispatcherComponent))]
    public static partial class GameAIComponentSystem
    {
        [Invoke(TimerInvokeType.GameAITimer)]
        public class GameAITimer : ATimer<GameAIComponent>
        {
            protected override void Run(GameAIComponent self)
            {
                try
                {
                    self.Check();
                }
                catch (Exception e)
                {
                    Log.Error($"game ai timer error: {self.Id}\n{e}");
                }
            }
        }

        [EntitySystem]
        private static void Awake(this GameAIComponent self, int aiConfigId)
        {
            self.AIConfigId = aiConfigId;
            self.Timer = self.Root().GetComponent<TimerComponent>().NewRepeatedTimer(1000, TimerInvokeType.GameAITimer, self);
        }

        [EntitySystem]
        private static void Destroy(this GameAIComponent self)
        {
            self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);
            self.CancellationTokenSource?.Cancel();
            self.CancellationTokenSource = null;
            self.Current = 0;
        }

        private static void Check(this GameAIComponent self)
        {
            if (self.Parent == null)
            {
                self.Root().GetComponent<TimerComponent>()?.Remove(ref self.Timer);
                return;
            }

            if (!Tables.Instance.DTGameAI.GameAIs.TryGetValue(self.AIConfigId, out var oneAI) || oneAI == null || oneAI.Count == 0)
            {
                return;
            }

            foreach (DRGameAI aiConfig in oneAI.Values)
            {
                AGameAIHandler handler = GameAIDispatcherComponent.Instance.Get(aiConfig.Name);
                if (handler == null)
                {
                    Log.Error($"game ai handler not found: {aiConfig.Name}");
                    continue;
                }

                int ret = handler.Check(self, aiConfig);
                if (ret != 0)
                {
                    continue;
                }

                if (self.Current == aiConfig.Id)
                {
                    break;
                }

                self.Cancel();
                CancellationTokenSource cts = new CancellationTokenSource();
                self.CancellationTokenSource = cts;
                self.Current = aiConfig.Id;
                handler.Execute(self, aiConfig, cts.Token).Forget();
                return;
            }
        }

        private static void Cancel(this GameAIComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            if (unit?.IScene != null)
            {
                unit.StopMove(false);
            }

            self.CancellationTokenSource?.Cancel();
            self.CancellationTokenSource = null;
            self.Current = 0;
        }
    }
}
