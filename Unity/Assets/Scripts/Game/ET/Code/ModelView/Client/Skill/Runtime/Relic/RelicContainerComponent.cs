using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(SkillUnit))]
    public partial class RelicContainerComponent : Entity, IAwake, IDestroy
    {
        public int BattleCardConfigId;
        public List<long> RelicInstanceIds = new List<long>();
    }
}
