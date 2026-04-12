using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillUnit))]
    [FriendOf(typeof(SkillUnit))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(SkillCardDeckComponent))]
    [FriendOf(typeof(RelicContainerComponent))]
    public static partial class SkillUnitSystem
    {
        [EntitySystem]
        private static void Awake(this SkillUnit self)
        {
            self.AddComponent<AbilitySystemComponent>();
            self.AddComponent<SkillCardDeckComponent>();
            self.AddComponent<RelicContainerComponent>();
            self.InitFromTable();
        }

        public static void InitFromTable(this SkillUnit self)
        {
            Unit unit = self.Unit.As();
            if (unit == null)
            {
                return;
            }

            AbilitySystemComponent asc = self.ASC.As();
            if (asc == null)
            {
                return;
            }

            UnitType unitType = (UnitType)unit.Config().Type;
            self.InitUnitTypeTags(asc);

            switch (unitType)
            {
                case UnitType.Player:
                    self.InitPlayerFromTable(asc, unit.ConfigId);
                    return;
                case UnitType.Monster:
                    self.InitMonsterFromTable(asc, unit.ConfigId);
                    return;
                default:
                    Log.Warning($"[SkillUnit] Unsupported unit type: {(byte)unitType}, UnitConfigId: {unit.ConfigId}");
                    return;
            }
        }

        public static DRHero GetHeroData(this SkillUnit self, int id)
        {
            var heroTable = Tables.Instance.DTHero;
            if (heroTable?.DataList == null)
            {
                return null;
            }

            return heroTable.Get(id);
        }

        public static DRMonster GetMonsterData(this SkillUnit self, int id)
        {
            var monsterTable = Tables.Instance.DTMonster;
            if (monsterTable?.DataList == null)
            {
                return null;
            }

            return monsterTable.Get(id);
        }

        private static void InitPlayerFromTable(this SkillUnit self, AbilitySystemComponent asc, int unitConfigId)
        {
            DRHero heroData = self.GetHeroData(unitConfigId);
            if (heroData == null)
            {
                Log.Warning($"[SkillUnit] Missing hero config, UnitConfigId: {unitConfigId}");
                return;
            }

            DRBattleCardConfig battleCardConfig = heroData.BattleCardConfigId_Ref
                ?? Tables.Instance.DTBattleCardConfig.GetOrDefault(heroData.BattleCardConfigId);
            if (battleCardConfig == null)
            {
                Log.Warning($"[SkillUnit] Missing battle card config, HeroId: {heroData.Id}, BattleCardConfigId: {heroData.BattleCardConfigId}");
                self.GrantSkills(asc, heroData.ActiveSkill);
                self.GrantSkills(asc, heroData.PassiveSkill, true);
                return;
            }

            self.SkillCardDeck.As()?.Initialize(battleCardConfig);
            self.RelicContainer.As()?.Initialize(battleCardConfig);

            Dictionary<int, GameplayAbilitySpec> grantedSpecs = new Dictionary<int, GameplayAbilitySpec>();
            self.GrantSkills(asc, heroData.ActiveSkill, grantedSpecs);
            self.GrantSkills(asc, heroData.PassiveSkill, grantedSpecs);
            self.CreatePlayerCards(grantedSpecs, heroData.ActiveSkill);
            self.CreatePlayerCards(grantedSpecs, heroData.PassiveSkill);
            self.SkillCardDeck.As()?.DrawCards(self.SkillCardDeck.As()?.DrawCount ?? 0);

            SkillDiagFileLogger.Log($"[PlayerCardInit] UnitConfigId={unitConfigId} BattleCardConfigId={battleCardConfig.Id} DrawPileCount={self.SkillCardDeck.As()?.DrawPileCardIds.Count ?? 0} HandCount={self.SkillCardDeck.As()?.HandCardIds.Count ?? 0} RelicCount={self.RelicContainer.As()?.RelicInstanceIds.Count ?? 0}");
        }

        private static void InitMonsterFromTable(this SkillUnit self, AbilitySystemComponent asc, int unitConfigId)
        {
            DRMonster monsterData = self.GetMonsterData(unitConfigId);
            if (monsterData == null)
            {
                Log.Warning($"[SkillUnit] Missing monster config, UnitConfigId: {unitConfigId}");
                return;
            }

            self.GrantSkills(asc, monsterData.ActiveSkill);
            self.GrantSkills(asc, monsterData.PassiveSkill, true);
        }

        private static void InitUnitTypeTags(this SkillUnit self, AbilitySystemComponent asc)
        {
            switch ((UnitType)self.Unit.As().Config().Type)
            {
                case UnitType.Player:
                    asc.OwnedTags.AddTag(GameplayTagLibrary.unitType_hero);
                    break;
                case UnitType.Monster:
                    asc.OwnedTags.AddTag(GameplayTagLibrary.unitType_monster);
                    break;
            }
        }

        private static void CreatePlayerCards(this SkillUnit self, Dictionary<int, GameplayAbilitySpec> grantedSpecs, int[] skillIds)
        {
            SkillCardDeckComponent deck = self.SkillCardDeck.As();
            if (deck == null || skillIds == null)
            {
                return;
            }

            foreach (int skillId in skillIds)
            {
                if (!grantedSpecs.TryGetValue(skillId, out GameplayAbilitySpec spec) || spec == null)
                {
                    continue;
                }

                DRSkill skillConfig = Tables.Instance.DTSkill.GetOrDefault(skillId);
                if (skillConfig == null)
                {
                    Log.Warning($"[CardDeck] Missing skill config, SkillId: {skillId}");
                    continue;
                }

                int copies = UnityEngine.Mathf.Max(skillConfig.CardCopies, 1);
                for (int i = 0; i < copies; i++)
                {
                    deck.AddCard(skillId, spec, skillConfig);
                }
            }
        }

        private static void GrantSkills(this SkillUnit self, AbilitySystemComponent asc, int[] skillIds, Dictionary<int, GameplayAbilitySpec> grantedSpecs)
        {
            if (skillIds == null)
            {
                return;
            }

            var tbSkill = Tables.Instance.DTSkill;
            var skillDataCenter = SkillDataCenter.Instance;
            if (skillDataCenter == null)
            {
                Log.Warning("[SkillUnit] SkillDataCenter is not initialized.");
                return;
            }

            foreach (int skillId in skillIds)
            {
                DRSkill skillData = tbSkill.GetOrDefault(skillId);
                if (skillData == null)
                {
                    Log.Warning($"[SkillUnit] Missing skill config, SkillId: {skillId}");
                    continue;
                }

                SkillData graphData = skillDataCenter.GetSkillGraph(skillData.Id.ToString());
                if (graphData == null)
                {
                    Log.Warning($"[SkillUnit] Missing skill graph, SkillId: {skillId}, UnitConfigId: {self.Unit.As()?.ConfigId ?? 0}");
                    continue;
                }

                GameplayAbilitySpec spec = asc.GrantAbility(graphData);
                if (spec != null && !grantedSpecs.ContainsKey(skillId))
                {
                    grantedSpecs.Add(skillId, spec);
                }
            }
        }

        private static void GrantSkills(this SkillUnit self, AbilitySystemComponent asc, int[] skillIds, bool autoActivate = false)
        {
            if (skillIds == null)
            {
                return;
            }

            var tbSkill = Tables.Instance.DTSkill;
            var skillDataCenter = SkillDataCenter.Instance;
            if (skillDataCenter == null)
            {
                Log.Warning("[SkillUnit] SkillDataCenter is not initialized.");
                return;
            }

            List<GameplayAbilitySpec> pendingActivationSpecs = autoActivate ? new List<GameplayAbilitySpec>() : null;
            foreach (int skillId in skillIds)
            {
                DRSkill skillData = tbSkill.GetOrDefault(skillId);
                if (skillData == null)
                {
                    Log.Warning($"[SkillUnit] Missing skill config, SkillId: {skillId}");
                    continue;
                }

                SkillData graphData = skillDataCenter.GetSkillGraph(skillData.Id.ToString());
                if (graphData == null)
                {
                    Log.Warning($"[SkillUnit] Missing skill graph, SkillId: {skillId}, UnitConfigId: {self.Unit.As()?.ConfigId ?? 0}");
                    continue;
                }

                GameplayAbilitySpec spec = asc.GrantAbility(graphData);
                if (autoActivate && spec != null)
                {
                    pendingActivationSpecs.Add(spec);
                }
            }

            if (!autoActivate || pendingActivationSpecs == null)
            {
                return;
            }

            foreach (GameplayAbilitySpec spec in pendingActivationSpecs)
            {
                bool activated = asc.TryActivateAbility(spec);
                if (!activated)
                {
                    Log.Warning($"[SkillUnit] Passive skill auto activation failed, SkillId: {spec?.GetSkillNumericId() ?? 0}, UnitConfigId: {self.Unit.As()?.ConfigId ?? 0}");
                }
            }
        }
    }
}
