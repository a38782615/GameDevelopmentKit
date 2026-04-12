using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(RelicContainerComponent))]
    [FriendOf(typeof(RelicContainerComponent))]
    [FriendOf(typeof(RelicRuntime))]
    public static partial class RelicContainerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RelicContainerComponent self)
        {
            self.ResetRuntimeState();
        }

        [EntitySystem]
        private static void Destroy(this RelicContainerComponent self)
        {
            self.ResetRuntimeState();
        }

        public static void Initialize(this RelicContainerComponent self, DRBattleCardConfig battleCardConfig)
        {
            self.ResetRuntimeState();
            if (battleCardConfig == null)
            {
                return;
            }

            self.BattleCardConfigId = battleCardConfig.Id;
            if (battleCardConfig.RelicIds == null)
            {
                return;
            }

            foreach (int relicId in battleCardConfig.RelicIds)
            {
                DRRelic relicConfig = Tables.Instance.DTRelic.GetOrDefault(relicId);
                if (relicConfig == null)
                {
                    Log.Warning($"[Relic] Missing relic config, RelicId: {relicId}, BattleCardConfigId: {battleCardConfig.Id}");
                    continue;
                }

                RelicRuntime relic = self.AddChildWithId<RelicRuntime, long>(relicId, relicId);
                relic.RelicId = relicConfig.Id;
                relic.EffectType = relicConfig.EffectType;
                relic.EffectValue = relicConfig.EffectValue;
                relic.TriggerType = relicConfig.TriggerType;
                self.RelicInstanceIds.Add(relicId);
            }
        }

        private static void ResetRuntimeState(this RelicContainerComponent self)
        {
            List<RelicRuntime> relics = new List<RelicRuntime>();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is RelicRuntime relic)
                {
                    relics.Add(relic);
                }
            }

            foreach (RelicRuntime relic in relics)
            {
                relic.Dispose();
            }

            self.BattleCardConfigId = 0;
            self.RelicInstanceIds.Clear();
        }
    }
}
