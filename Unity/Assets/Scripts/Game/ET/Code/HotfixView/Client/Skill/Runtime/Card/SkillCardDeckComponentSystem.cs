using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(SkillCardRuntime))]
    [FriendOf(typeof(SkillUnit))]
    [FriendOf(typeof(GameplayAbilitySpec))]
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
            spec.BindCardInstance(cardInstanceId);
            return card;
        }

        public static void DrawCards(this SkillCardDeckComponent self, int count)
        {
            if (count <= 0)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (!self.TryDrawOne())
                {
                    break;
                }
            }
        }

        public static bool TryCastCard(this SkillCardDeckComponent self, long cardInstanceId)
        {
            SkillCardRuntime card = self.GetChild<SkillCardRuntime>(cardInstanceId);
            if (card == null)
            {
                Log.Warning($"[CardDeck] Missing card runtime, CardInstanceId: {cardInstanceId}");
                return false;
            }

            if (card.Zone != SkillCardZone.Hand)
            {
                Log.Warning($"[CardDeck] Card is not in hand, CardInstanceId: {cardInstanceId}, Zone: {card.Zone}");
                return false;
            }

            GameplayAbilitySpec spec = card.SpecRef.As();
            AbilitySystemComponent asc = self.GetParent<SkillUnit>()?.ASC.As();
            if (spec == null || asc == null)
            {
                Log.Warning($"[CardDeck] Missing cast context, CardInstanceId: {cardInstanceId}");
                return false;
            }

            if (card.TriggerType == 1)
            {
                self.MoveCardToZone(card, SkillCardZone.Ability);
                SkillDiagFileLogger.Log($"[CardDeck] Passive card entered ability zone, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}");
                return true;
            }

            spec.SetActivatingCardInstance(cardInstanceId);
            spec.ActivatingCardResolvedCostMp = card.GetResolvedCostMp();
            bool activated = asc.TryActivateAbility(spec);
            if (!activated)
            {
                spec.SetActivatingCardInstance(0);
                spec.ActivatingCardResolvedCostMp = 0f;
                Log.Warning($"[CardDeck] Active card cast failed, CardInstanceId: {cardInstanceId}, SkillId: {card.SkillId}");
                return false;
            }

            self.MoveCardToZone(card, SkillCardZone.DiscardPile);
            SkillDiagFileLogger.Log($"[CardDeck] Active card cast success, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}, CostMp={card.GetResolvedCostMp()}");
            return true;
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
                card.SpecRef.As()?.UnbindCardInstance(card.CardInstanceId);
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

        private static bool TryDrawOne(this SkillCardDeckComponent self)
        {
            if (self.DrawPileCardIds.Count <= 0)
            {
                return false;
            }

            long cardInstanceId = self.DrawPileCardIds[0];
            SkillCardRuntime card = self.GetChild<SkillCardRuntime>(cardInstanceId);
            if (card == null)
            {
                self.DrawPileCardIds.RemoveAt(0);
                return false;
            }

            self.MoveCardToZone(card, SkillCardZone.Hand);
            return true;
        }

        private static void MoveCardToZone(this SkillCardDeckComponent self, SkillCardRuntime card, SkillCardZone zone)
        {
            if (card == null)
            {
                return;
            }

            self.RemoveCardFromAllZones(card.CardInstanceId);
            card.Zone = zone;
            self.GetZoneList(zone).Add(card.CardInstanceId);
        }

        private static void RemoveCardFromAllZones(this SkillCardDeckComponent self, long cardInstanceId)
        {
            self.DrawPileCardIds.Remove(cardInstanceId);
            self.HandCardIds.Remove(cardInstanceId);
            self.AbilityCardIds.Remove(cardInstanceId);
            self.DiscardPileCardIds.Remove(cardInstanceId);
            self.DestroyedCardIds.Remove(cardInstanceId);
        }

        private static List<long> GetZoneList(this SkillCardDeckComponent self, SkillCardZone zone)
        {
            return zone switch
            {
                SkillCardZone.DrawPile => self.DrawPileCardIds,
                SkillCardZone.Hand => self.HandCardIds,
                SkillCardZone.Ability => self.AbilityCardIds,
                SkillCardZone.DiscardPile => self.DiscardPileCardIds,
                SkillCardZone.Destroyed => self.DestroyedCardIds,
                _ => self.DrawPileCardIds,
            };
        }
    }
}
