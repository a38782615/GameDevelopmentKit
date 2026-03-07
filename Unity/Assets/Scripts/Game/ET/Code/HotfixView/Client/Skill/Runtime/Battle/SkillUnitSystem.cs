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
            var asc = self.ASC.As();
            if (asc == null) return;

            var data = Tables.Instance.DTUnit.GetOrDefault(self.Unit.As().ConfigId);
            self.InitAttributes(asc, data.InitialAttribute);
            self.GrantSkills(asc, data.ActiveSkill);
            self.GrantSkills(asc, data.PassiveSkill);
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

        private static void GrantSkills(this SkillUnit self, AbilitySystemComponent asc, int[] skillIds)
        {
            if (skillIds == null) return;

            var tbSkill = Tables.Instance.DTSkill;
            foreach (var skillId in skillIds)
            {
                var skillData = tbSkill.GetOrDefault(skillId);
                if (skillData == null)
                {
                    Log.Warning($"[Unit] 技能表中找不到ID: {skillId}");
                    continue;
                }
                var graphData = SkillDataCenter.Instance.GetSkillGraph(skillData.Id.ToString());
                asc.GrantAbility(graphData);
            }
        }
    }
}