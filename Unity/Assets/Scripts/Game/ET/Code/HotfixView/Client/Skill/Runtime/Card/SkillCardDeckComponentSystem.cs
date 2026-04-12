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

        [EntitySystem]
        private static void Update(this SkillCardDeckComponent self)
        {
            self.Tick(UnityEngine.Time.deltaTime);
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
            self.InitMp = ruleConfig.InitMp;
            self.CycleSeconds = ruleConfig.CycleSeconds;
            self.CurrentCycleTime = ruleConfig.CycleSeconds;
            self.MoveDrainMpPerSecond = ruleConfig.MoveDrainMpPerSecond;
            self.CurrentMoveDrainTime = 0f;
            self.PassiveTriggerIntervalSeconds = ruleConfig.PassiveTriggerIntervalSeconds;
            self.PassiveTriggerElapsed = 0f;
            self.IsMoveDraining = false;
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
            SkillDiagFileLogger.Log($"[CardDeck] Add card, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}, Zone={card.Zone}, BaseCostMp={card.BaseCostMp:F3}, TriggerType={card.TriggerType}");
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

            float resolvedCostMp = card.GetResolvedCostMp();
            if (!self.CanAffordCardCost(asc, resolvedCostMp))
            {
                Log.Warning($"[CardDeck] MP is not enough, CardInstanceId: {cardInstanceId}, SkillId: {card.SkillId}, CostMp: {resolvedCostMp}");
                return false;
            }

            if (card.TriggerType == 1)
            {
                if (!self.PayCardCost(asc, resolvedCostMp))
                {
                    return false;
                }

                self.MoveCardToZone(card, SkillCardZone.Ability);
                SkillDiagFileLogger.Log($"[CardDeck] Passive card entered ability zone, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}, CostMp={resolvedCostMp}");
                return true;
            }

            spec.SetActivatingCardInstance(cardInstanceId);
            spec.ActivatingCardResolvedCostMp = resolvedCostMp;
            bool activated = asc.TryActivateAbility(spec);
            if (!activated)
            {
                spec.SetActivatingCardInstance(0);
                spec.ActivatingCardResolvedCostMp = 0f;
                Log.Warning($"[CardDeck] Active card cast failed, CardInstanceId: {cardInstanceId}, SkillId: {card.SkillId}");
                return false;
            }

            if (!self.PayCardCost(asc, resolvedCostMp))
            {
                spec.SetActivatingCardInstance(0);
                spec.ActivatingCardResolvedCostMp = 0f;
                Log.Warning($"[CardDeck] MP pay failed after activation, CardInstanceId: {cardInstanceId}, SkillId: {card.SkillId}");
                return false;
            }

            self.MoveCardToZone(card, SkillCardZone.DiscardPile);
            SkillDiagFileLogger.Log($"[CardDeck] Active card cast success, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}, CostMp={resolvedCostMp}");
            return true;
        }

        public static float GetResolvedCostMp(this SkillCardRuntime self)
        {
            return self.HasOverrideCostMp ? self.OverrideCostMp : self.BaseCostMp;
        }

        public static void ResetCostOverride(this SkillCardRuntime self, string source = null)
        {
            if (self == null)
            {
                return;
            }

            float beforeCost = self.GetResolvedCostMp();
            self.HasOverrideCostMp = false;
            self.OverrideCostMp = self.BaseCostMp;
            float afterCost = self.GetResolvedCostMp();
            SkillDiagFileLogger.Log($"[CardDeck] Card cost override reset, CardInstanceId={self.CardInstanceId}, SkillId={self.SkillId}, Source={source ?? "Unknown"}, BeforeCost={beforeCost:F3}, AfterCost={afterCost:F3}");
        }

        public static void SetOverrideCostMp(this SkillCardRuntime self, float overrideCostMp, string source = null)
        {
            if (self == null)
            {
                return;
            }

            float beforeCost = self.GetResolvedCostMp();
            self.HasOverrideCostMp = true;
            self.OverrideCostMp = UnityEngine.Mathf.Max(0f, overrideCostMp);
            float afterCost = self.GetResolvedCostMp();
            SkillDiagFileLogger.Log($"[CardDeck] Card cost override set, CardInstanceId={self.CardInstanceId}, SkillId={self.SkillId}, Source={source ?? "Unknown"}, BeforeCost={beforeCost:F3}, AfterCost={afterCost:F3}");
        }

        public static void AddOverrideCostDeltaMp(this SkillCardRuntime self, float deltaMp, string source = null)
        {
            if (self == null)
            {
                return;
            }

            float resolvedCost = self.GetResolvedCostMp();
            self.SetOverrideCostMp(resolvedCost + deltaMp, source);
        }

        public static void Tick(this SkillCardDeckComponent self, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            self.TickMoveDrain(deltaTime);
            self.TickCycle(deltaTime);
            self.TickPassiveCards(deltaTime);
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
            self.InitMp = 0f;
            self.CycleSeconds = 0f;
            self.CurrentCycleTime = 0f;
            self.MoveDrainMpPerSecond = 0f;
            self.CurrentMoveDrainTime = 0f;
            self.PassiveTriggerIntervalSeconds = 0f;
            self.PassiveTriggerElapsed = 0f;
            self.IsMoveDraining = false;
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
                self.ReshuffleDiscardIntoDrawPile();
            }

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

            SkillCardZone targetZone = self.HandCardIds.Count < self.HandLimit || self.HandLimit <= 0
                ? SkillCardZone.Hand
                : SkillCardZone.DiscardPile;
            self.MoveCardToZone(card, targetZone);
            SkillDiagFileLogger.Log($"[CardDeck] Draw card, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}, TargetZone={targetZone}, DrawPileRemaining={self.DrawPileCardIds.Count}, HandCount={self.HandCardIds.Count}, DiscardCount={self.DiscardPileCardIds.Count}");

            if (targetZone == SkillCardZone.DiscardPile)
            {
                SkillDiagFileLogger.Log($"[CardDeck] Hand limit overflow to discard, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}");
            }

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

        private static bool CanAffordCardCost(this SkillCardDeckComponent self, AbilitySystemComponent asc, float costMp)
        {
            var attributes = asc?.Attributes;
            if (attributes == null)
            {
                return false;
            }

            if (costMp <= 0f)
            {
                return true;
            }

            float currentMp = attributes.GetCurrentValue(global::ET.NumericType.Mp);
            return currentMp >= costMp;
        }

        private static bool PayCardCost(this SkillCardDeckComponent self, AbilitySystemComponent asc, float costMp)
        {
            return self.TryPayMp(asc, costMp, out _, out _);
        }

        private static bool TryPayMp(this SkillCardDeckComponent self, AbilitySystemComponent asc, float costMp, out float beforeMp, out float afterMp)
        {
            var attributes = asc?.Attributes;
            beforeMp = 0f;
            afterMp = 0f;
            if (attributes == null)
            {
                return false;
            }

            beforeMp = attributes.GetCurrentValue(global::ET.NumericType.Mp);
            afterMp = beforeMp;
            if (costMp <= 0f)
            {
                return true;
            }

            if (beforeMp < costMp)
            {
                return false;
            }

            afterMp = UnityEngine.Mathf.Max(0f, beforeMp - costMp);
            return attributes.SetCurrentValue(global::ET.NumericType.Mp, afterMp);
        }

        private static void TickMoveDrain(this SkillCardDeckComponent self, float deltaTime)
        {
            if (self.MoveDrainMpPerSecond <= 0f)
            {
                self.StopMoveDrain();
                return;
            }

            SkillUnit skillUnit = self.GetParent<SkillUnit>();
            global::ET.Unit unit = skillUnit?.Unit.As();
            AbilitySystemComponent asc = skillUnit?.ASC.As();
            if (unit == null || asc == null)
            {
                self.StopMoveDrain();
                return;
            }

            if (!self.IsUnitMoving(unit))
            {
                self.StopMoveDrain();
                return;
            }

            if (!self.IsMoveDraining)
            {
                self.IsMoveDraining = true;
                self.CurrentMoveDrainTime = 0f;
                SkillDiagFileLogger.Log($"[CardDeck] Move drain start, UnitConfigId={unit.ConfigId}, Rate={self.MoveDrainMpPerSecond}");
            }

            self.CurrentMoveDrainTime += deltaTime;
            float drainMp = self.MoveDrainMpPerSecond * deltaTime;
            if (drainMp <= 0f)
            {
                return;
            }

            if (!self.TryPayMp(asc, drainMp, out float beforeMp, out float afterMp))
            {
                return;
            }

            SkillDiagFileLogger.Log($"[CardDeck] Move drain tick, UnitConfigId={unit.ConfigId}, DeltaTime={deltaTime:F3}, DrainMp={drainMp:F3}, BeforeMp={beforeMp:F3}, AfterMp={afterMp:F3}, MoveElapsed={self.CurrentMoveDrainTime:F3}");
        }

        private static bool IsUnitMoving(this SkillCardDeckComponent self, global::ET.Unit unit)
        {
            global::ET.Move2DComponent move2DComponent = unit.GetComponent<global::ET.Move2DComponent>();
            if (move2DComponent != null && !global::ET.Move2DComponentSystem.IsArrived(move2DComponent))
            {
                return true;
            }

            global::ET.MoveComponent moveComponent = unit.GetComponent<global::ET.MoveComponent>();
            return moveComponent != null && !global::ET.MoveComponentSystem.IsArrived(moveComponent);
        }

        private static void StopMoveDrain(this SkillCardDeckComponent self)
        {
            if (!self.IsMoveDraining)
            {
                return;
            }

            SkillDiagFileLogger.Log($"[CardDeck] Move drain stop, Elapsed={self.CurrentMoveDrainTime:F3}");
            self.IsMoveDraining = false;
            self.CurrentMoveDrainTime = 0f;
        }

        private static void TickCycle(this SkillCardDeckComponent self, float deltaTime)
        {
            if (self.CycleSeconds <= 0f)
            {
                return;
            }

            self.CurrentCycleTime -= deltaTime;
            if (self.CurrentCycleTime > 0f)
            {
                return;
            }

            self.ExecuteCycle();
        }

        private static void ExecuteCycle(this SkillCardDeckComponent self)
        {
            self.ResetMpToMax();
            self.DiscardHandCards();
            self.DrawCards(self.DrawCount);
            self.CurrentCycleTime = self.CycleSeconds;
            self.PassiveTriggerElapsed = 0f;
            SkillDiagFileLogger.Log($"[CardDeck] Cycle reset, BattleCardConfigId={self.BattleCardConfigId}, DrawPile={self.DrawPileCardIds.Count}, Hand={self.HandCardIds.Count}, Discard={self.DiscardPileCardIds.Count}, Ability={self.AbilityCardIds.Count}");
        }

        private static void TickPassiveCards(this SkillCardDeckComponent self, float deltaTime)
        {
            if (self.AbilityCardIds.Count <= 0 || self.PassiveTriggerIntervalSeconds <= 0f)
            {
                return;
            }

            self.PassiveTriggerElapsed += deltaTime;
            while (self.PassiveTriggerElapsed >= self.PassiveTriggerIntervalSeconds)
            {
                self.PassiveTriggerElapsed -= self.PassiveTriggerIntervalSeconds;
                self.TriggerAbilityZoneCards();
            }
        }

        private static void TriggerAbilityZoneCards(this SkillCardDeckComponent self)
        {
            AbilitySystemComponent asc = self.GetParent<SkillUnit>()?.ASC.As();
            if (asc == null)
            {
                return;
            }

            List<long> abilityCardIds = new List<long>(self.AbilityCardIds);
            foreach (long cardInstanceId in abilityCardIds)
            {
                SkillCardRuntime card = self.GetChild<SkillCardRuntime>(cardInstanceId);
                GameplayAbilitySpec spec = card?.SpecRef.As();
                if (card == null || spec == null)
                {
                    continue;
                }

                spec.SetActivatingCardInstance(cardInstanceId);
                spec.ActivatingCardResolvedCostMp = 0f;
                bool activated = asc.TryActivateAbility(spec);
                if (activated)
                {
                    SkillDiagFileLogger.Log($"[CardDeck] Ability zone trigger success, CardInstanceId={card.CardInstanceId}, SkillId={card.SkillId}");
                }
            }
        }

        private static void DiscardHandCards(this SkillCardDeckComponent self)
        {
            List<long> handCardIds = new List<long>(self.HandCardIds);
            foreach (long cardInstanceId in handCardIds)
            {
                SkillCardRuntime card = self.GetChild<SkillCardRuntime>(cardInstanceId);
                if (card == null)
                {
                    continue;
                }

                self.MoveCardToZone(card, SkillCardZone.DiscardPile);
            }
        }

        private static void ReshuffleDiscardIntoDrawPile(this SkillCardDeckComponent self)
        {
            if (self.DiscardPileCardIds.Count <= 0)
            {
                return;
            }

            List<long> shuffled = new List<long>(self.DiscardPileCardIds);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
            }

            self.DiscardPileCardIds.Clear();
            foreach (long cardInstanceId in shuffled)
            {
                SkillCardRuntime card = self.GetChild<SkillCardRuntime>(cardInstanceId);
                if (card == null)
                {
                    continue;
                }

                self.MoveCardToZone(card, SkillCardZone.DrawPile);
            }

            SkillDiagFileLogger.Log($"[CardDeck] Reshuffle discard into draw pile, Count={self.DrawPileCardIds.Count}");
        }

        private static void ResetMpToMax(this SkillCardDeckComponent self)
        {
            AbilitySystemComponent asc = self.GetParent<SkillUnit>()?.ASC.As();
            var attributes = asc?.Attributes;
            if (attributes == null)
            {
                return;
            }

            float maxMp = attributes.GetCurrentValue(global::ET.NumericType.MaxMp);
            attributes.SetCurrentValue(global::ET.NumericType.Mp, maxMp);
        }

        public static void InitializeMp(this SkillCardDeckComponent self)
        {
            AbilitySystemComponent asc = self.GetParent<SkillUnit>()?.ASC.As();
            var attributes = asc?.Attributes;
            if (attributes == null)
            {
                return;
            }

            float maxMp = attributes.GetCurrentValue(global::ET.NumericType.MaxMp);
            float targetMp = self.InitMp > 0f ? UnityEngine.Mathf.Min(self.InitMp, maxMp) : maxMp;
            attributes.SetCurrentValue(global::ET.NumericType.Mp, targetMp);
            SkillDiagFileLogger.Log($"[CardDeck] Initialize MP, BattleCardConfigId={self.BattleCardConfigId}, InitMp={self.InitMp:F3}, MaxMp={maxMp:F3}, AppliedMp={targetMp:F3}");
        }
    }
}
