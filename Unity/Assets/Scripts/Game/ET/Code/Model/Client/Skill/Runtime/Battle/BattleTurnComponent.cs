using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public partial class BattleTurnComponent : Entity, IAwake, IDestroy
    {
        public readonly HashSet<long> ActiveAttackSpecs = new HashSet<long>();
    }
}
