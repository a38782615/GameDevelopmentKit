using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillUnit))]
    [FriendOf(typeof(SkillUnit))]
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayAbilitySpec))]
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
                    self.InitPlayerFromTable(asc, unit.Id);
                    return;
                case UnitType.Monster:
                    self.InitMonsterFromTable(asc, unit.Id);
                    return;
                default:
                    Log.Warning($"[SkillUnit] Unsupported unit type: {(byte)unitType}, UnitConfigId: {unit.ConfigId}");
                    return;
            }
        }

        public static DRMonster GetMonsterData(this SkillUnit self, long id)
        {
            var monsterTable = Tables.Instance.DTMonster;
            if (monsterTable?.DataList == null)
            {
                return null;
            }

            return monsterTable.Get(id);
        }

        private static void InitPlayerFromTable(this SkillUnit self, AbilitySystemComponent asc, long playerId)
        {
            GameDataMgrComponent gameDataMgrComponent = self.Root().GetComponent<GameDataMgrComponent>();
            PlayerSkillDataComponent playerSkillDataComponent = gameDataMgrComponent?.GetPlayerSkillDataComponent();
            if (playerSkillDataComponent == null)
            {
                Log.Warning($"[SkillUnit] Missing PlayerSkillDataComponent, PlayerId: {playerId}");
                return;
            }

            self.GrantPlayerSkills(asc, playerSkillDataComponent.GetEquippedActiveSkills());
            self.GrantPlayerSkills(asc, playerSkillDataComponent.GetEquippedPassiveSkills(), true);
        }

        private static void InitMonsterFromTable(this SkillUnit self, AbilitySystemComponent asc, long id)
        {
            DRMonster monsterData = self.GetMonsterData(id);
            if (monsterData == null)
            {
                Log.Warning($"[SkillUnit] Missing monster config, id: {id}");
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

        private static void GrantSkills(
            this SkillUnit self,
            AbilitySystemComponent asc,
            int[] skillIds,
            bool autoActivate = false,
            int[] skillLevels = null)
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
            for (int i = 0; i < skillIds.Length; ++i)
            {
                int skillId = skillIds[i];
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
                if (spec != null && skillLevels != null && i < skillLevels.Length)
                {
                    spec.Level = skillLevels[i];
                }

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

        private static void GrantPlayerSkills(
            this SkillUnit self,
            AbilitySystemComponent asc,
            XList<PlayerSkillData> playerSkills,
            bool autoActivate = false)
        {
            if (playerSkills == null || playerSkills.Count == 0)
            {
                return;
            }

            int[] configIds = new int[playerSkills.Count];
            int[] levels = new int[playerSkills.Count];
            for (int i = 0; i < playerSkills.Count; ++i)
            {
                configIds[i] = playerSkills[i].ConfigId;
                levels[i] = playerSkills[i].Level;
            }

            self.GrantSkills(asc, configIds, autoActivate, levels);
        }
    }
}
