using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardRuntime))]
    public static partial class SkillCardDeckComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SkillCardDeckComponent self)
        {
            self.NextCardInstanceId = 1;
            self.ResetRuntimeState();
        }

        [EntitySystem]
        private static void Destroy(this SkillCardDeckComponent self)
        {
            self.ResetRuntimeState();
        }

        public static void Initialize(this SkillCardDeckComponent self, DRBattleCardConfig battleCardConfig)
        {
            self.ResetRuntimeState();
            if (battleCardConfig == null)
            {
                return;
            }

            self.BattleCardConfigId = battleCardConfig.Id;
            self.SkillCardRuleId = battleCardConfig.SkillCardRuleId;

            DRSkillCardRule ruleConfig = battleCardConfig.SkillCardRuleId_Ref
                ?? Tables.Instance.DTSkillCardRule.GetOrDefault(battleCardConfig.SkillCardRuleId);
            if (ruleConfig == null)
            {
                Log.Warning($"[CardDeck] Missing card rule config, BattleCardConfigId: {battleCardConfig.Id}, SkillCardRuleId: {battleCardConfig.SkillCardRuleId}");
                return;
            }

            self.DrawCount = ruleConfig.DrawCount;
            self.HandLimit = ruleConfig.HandLimit;
            self.CycleSeconds = ruleConfig.CycleSeconds;
            self.MoveDrainMpPerSecond = ruleConfig.MoveDrainMpPerSecond;
            self.PassiveTriggerIntervalSeconds = ruleConfig.PassiveTriggerIntervalSeconds;
        }

        public static SkillCardRuntime AddCard(this SkillCardDeckComponent self, int skillId, GameplayAbilitySpec spec, DRSkill skillConfig)
        {
            if (spec == null || skillConfig == null)
            {
                return null;
            }

            long cardInstanceId = self.NextCardInstanceId++;
            SkillCardRuntime card = self.AddChildWithId<SkillCardRuntime, long>(cardInstanceId, cardInstanceId);
            card.CardInstanceId = cardInstanceId;
            card.SkillId = skillId;
            card.SpecRef = spec;
            card.Zone = SkillCardZone.DrawPile;
            card.BaseCostMp = skillConfig.CardBaseCostMp;
            card.OverrideCostMp = skillConfig.CardBaseCostMp;
            card.HasOverrideCostMp = false;
            card.TriggerType = skillConfig.CardTriggerType;
            self.DrawPileCardIds.Add(cardInstanceId);
            return card;
        }

        public static float GetResolvedCostMp(this SkillCardRuntime self)
        {
            return self.HasOverrideCostMp ? self.OverrideCostMp : self.BaseCostMp;
        }

        private static void ResetRuntimeState(this SkillCardDeckComponent self)
        {
            List<SkillCardRuntime> cards = new List<SkillCardRuntime>();
            foreach (Entity entity in self.Children.Values)
            {
                if (entity is SkillCardRuntime card)
                {
                    cards.Add(card);
                }
            }

            foreach (SkillCardRuntime card in cards)
            {
                card.Dispose();
            }

            self.BattleCardConfigId = 0;
            self.SkillCardRuleId = 0;
            self.DrawCount = 0;
            self.HandLimit = 0;
            self.CycleSeconds = 0f;
            self.MoveDrainMpPerSecond = 0f;
            self.PassiveTriggerIntervalSeconds = 0f;
            self.DrawPileCardIds.Clear();
            self.HandCardIds.Clear();
            self.AbilityCardIds.Clear();
            self.DiscardPileCardIds.Clear();
            self.DestroyedCardIds.Clear();
        }
    }
}
