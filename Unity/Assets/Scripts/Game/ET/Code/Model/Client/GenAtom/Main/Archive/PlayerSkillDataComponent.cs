namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class PlayerSkillDataComponent : Entity, IAwake, IDestroy
    {
        public XList<PlayerSkillData> LearnedSkills;
        public XDictionary<int, PlayerSkillData> SkillDataById;
        public XList<PlayerSkillData> EquippedSkills;
        public XList<PlayerSkillData> EquippedActiveSkills;
        public XList<PlayerSkillData> EquippedPassiveSkills;
        public XList<PlayerSkillData> UpgradeableSkills;
    }
}
