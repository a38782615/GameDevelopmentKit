using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(TimeCueRuntimeComponent))]
    [FriendOf(typeof(TimeCueRuntimeComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayCueSpec))]

    public static partial class TimeCueRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TimeCueRuntimeComponent self)
        {
            self.TimeCues.Clear();
        }

        [EntitySystem]
        private static void Destroy(this TimeCueRuntimeComponent self)
        {
            self.StopAll();
            self.TimeCues.Clear();
        }

        // ============ 重置 ============

        public static void ResetAll(this TimeCueRuntimeComponent self)
        {
            foreach (var tc in self.TimeCues)
            {
                tc.HasStarted = false;
                tc.HasEnded = false;
                tc.TriggeredCueSpecs.Clear();
            }
        }

        // ============ 检查触发 ============

        public static void CheckTriggers(this TimeCueRuntimeComponent self, string skillId, string animationNodeGuid, float currentPlayTime, float animationDuration, SpecExecutionContext context)
        {
            if (context == null) return;

            foreach (var tc in self.TimeCues)
            {
                // 检查开始触发
                if (!tc.HasStarted && currentPlayTime >= tc.StartTime)
                {
                    tc.HasStarted = true;
                    var triggeredSpecs = context.ExecuteConnectedCueNodes(skillId, animationNodeGuid, tc.PortId);
                    foreach (var triggeredSpec in triggeredSpecs)
                    {
                        if (triggeredSpec != null && triggeredSpec.DestroyWithNode)
                        {
                            tc.TriggeredCueSpecs.Add(triggeredSpec);
                        }
                    }
                }

                // 检查结束触发
                if (tc.HasStarted && !tc.HasEnded)
                {
                    float effectiveEndTime = tc.EndTime < 0 ? animationDuration : tc.EndTime;
                    if (currentPlayTime >= effectiveEndTime)
                    {
                        tc.HasEnded = true;
                        self.StopTimeCueSpecs(tc);
                    }
                }
            }
        }

        // ============ 停止 ============

        private static void StopTimeCueSpecs(this TimeCueRuntimeComponent self, TimeCueRuntime timeCue)
        {
            foreach (var cueSpec in timeCue.TriggeredCueSpecs)
            {
                self.StopCue(cueSpec);
            }
            timeCue.TriggeredCueSpecs.Clear();
        }

        public static void StopAll(this TimeCueRuntimeComponent self)
        {
            foreach (var tc in self.TimeCues)
            {
                if (tc.HasStarted && !tc.HasEnded)
                {
                    self.StopTimeCueSpecs(tc);
                    tc.HasEnded = true;
                }
            }
        }

        private static void StopCue(this TimeCueRuntimeComponent self, GameplayCueSpec cueSpec)
        {
            if (cueSpec == null || string.IsNullOrEmpty(cueSpec.HandName))
            {
                return;
            }

            var handler = CueDispatcherComponent.Instance.Get(cueSpec.HandName);
            if (handler == null)
            {
                Log.Error($"CueHandler not found: {cueSpec.HandName}");
                return;
            }

            handler.Spec = cueSpec;
            handler.NodeData = cueSpec.GetCueNodeData();
            cueSpec.IsRunning = false;
            handler.StopCue();
        }
    }
}
