using System.Collections.Generic;

namespace ET.Client
{
    [EntitySystemOf(typeof(SkillUnit))]
    [FriendOf(typeof(SkillUnit))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
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
            var unit = self.Unit.As();
            if (unit == null) return;

            var asc = self.ASC.As();
            if (asc == null) return;

            var unitType = (UnitType)unit.Config().Type;

            self.InitUnitTypeTags(asc);


            switch (unitType)
            {
                case UnitType.Player:
                    {
                        var heroData = self.GetHeroData(unit.ConfigId);
                        if (heroData == null)
                        {
                            Log.Warning($"[Unit] 英雄表中找不到 UnitConfigId: {unit.ConfigId}");
                            return;
                        }

                        self.GrantSkills(asc, heroData.ActiveSkill);

                        self.GrantSkills(asc, heroData.PassiveSkill, true);

                        return;
                    }
                case UnitType.Monster:
                    {
                        var monsterData = self.GetMonsterData(unit.ConfigId);
                        if (monsterData == null)
                        {
                            Log.Warning($"[Unit] 怪物表中找不到 UnitConfigId: {unit.ConfigId}");
                            return;
                        }

                        self.GrantSkills(asc, monsterData.ActiveSkill);

                        self.GrantSkills(asc, monsterData.PassiveSkill, true);

                        return;
                    }
                default:
                    Log.Warning($"[Unit] 不支持的单位类型: {(byte)unitType}, UnitConfigId: {unit.ConfigId}");
                    return;
            }
        }

        public static global::ET.DRHero GetHeroData(this SkillUnit self, int unitConfigId)
        {
            var heroTable = Tables.Instance.DTHero;
            if (heroTable?.DataList == null)
            {
                return null;
            }

            foreach (var heroData in heroTable.DataList)
            {
                if (heroData.UnitConfigId == unitConfigId)
                {
                    return heroData;
                }
            }

            return null;
        }

        public static global::ET.DRMonster GetMonsterData(this SkillUnit self, int unitConfigId)
        {
            var monsterTable = Tables.Instance.DTMonster;
            if (monsterTable?.DataList == null)
            {
                return null;
            }

            foreach (var monsterData in monsterTable.DataList)
            {
                if (monsterData.UnitConfigId == unitConfigId)
                {
                    return monsterData;
                }
            }

            return null;
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

        private static void InitAttributes(this SkillUnit self, AbilitySystemComponent asc, (int, int)[] attributes)
        {
            if (attributes == null) return;

            foreach (var (typeId, value) in attributes)
            {
                var attrType = (AttrType)typeId;
                if (!asc.Attributes.HasAttribute(attrType))
                    asc.Attributes.AddAttribute(attrType, value);
            }
        }

        private static void GrantSkills(this SkillUnit self, AbilitySystemComponent asc, int[] skillIds, bool autoActivate = false)
        {
            if (skillIds == null) return;

            var tbSkill = Tables.Instance.DTSkill;
            var skillDataCenter = SkillDataCenter.Instance;
            if (skillDataCenter == null)
            {
                Log.Warning("[Unit] SkillDataCenter 未初始化，无法授予技能");
                return;
            }

            List<GameplayAbilitySpec> pendingActivationSpecs = autoActivate ? new List<GameplayAbilitySpec>() : null;
            foreach (var skillId in skillIds)
            {

                var skillData = tbSkill.GetOrDefault(skillId);
                if (skillData == null)
                {
                    Log.Warning($"[Unit] 技能表中找不到ID: {skillId}");
                    continue;
                }

                var graphData = skillDataCenter.GetSkillGraph(skillData.Id.ToString());

                if (graphData == null)
                {
                    Log.Warning($"[Unit] 技能图未注册，技能ID: {skillId}, UnitConfigId: {self.Unit.As()?.ConfigId ?? 0}");
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
                    Log.Warning($"[Unit] 被动技能自动激活失败 SkillId: {spec?.AbilityNodeData?.skillId ?? 0}, UnitConfigId: {self.Unit.As()?.ConfigId ?? 0}");
                }
            }
        }
    }
}
