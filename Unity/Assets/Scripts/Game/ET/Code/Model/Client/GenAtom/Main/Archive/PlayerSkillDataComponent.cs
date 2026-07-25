using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(GameDataMgrComponent))]
    public class PlayerSkillDataComponent : Entity, IAwake, IDestroy
    {
        public List<PlayerSkillData> LearnedSkills = new List<PlayerSkillData>();
    }
}
