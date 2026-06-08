using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillUnit))]
    [FriendOf(typeof(SkillUnit))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static partial class SkillUnitSystem
    {
        [EntitySystem]
        private static void Awake(this SkillUnit self)
        {
            self.AddComponent<AbilitySystemComponent>();
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

            self.GrantSkills(asc, heroData.ActiveSkill);
            self.GrantSkills(asc, heroData.PassiveSkill, true);
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
