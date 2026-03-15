namespace ET.Client
{
    [EntitySystemOf(typeof(TimeEffectRuntimeComponent))]
    [FriendOf(typeof(TimeEffectRuntimeComponent))]
    public static partial class TimeEffectRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this TimeEffectRuntimeComponent self)
        {
            self.TimeEffects.Clear();
        }

        [EntitySystem]
        private static void Destroy(this TimeEffectRuntimeComponent self)
        {
            self.TimeEffects.Clear();
        }

        // ============ 重置 ============
        public static void ResetAll(this TimeEffectRuntimeComponent self)
        {
            foreach (var te in self.TimeEffects)
            {
                te.HasTriggered = false;
            }
        }

        // ============ 检查触发 ============

        public static void CheckTriggers(this TimeEffectRuntimeComponent self, string skillId, string animationNodeGuid, float currentPlayTime, SpecExecutionContext context)
        {
            if (context == null) return;

            foreach (var te in self.TimeEffects)
            {
                if (!te.HasTriggered && currentPlayTime >= te.TriggerTime)
                {
                    te.HasTriggered = true;
                    context.ExecuteConnectedNodes(skillId, animationNodeGuid, te.PortId);
                }
            }
        }
    }
}
