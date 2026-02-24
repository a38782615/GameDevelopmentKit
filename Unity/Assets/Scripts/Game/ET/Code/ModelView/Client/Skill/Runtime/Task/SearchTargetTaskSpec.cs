using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 搜索目标任务Spec
    /// 使用Physics2D进行碰撞检测
    /// </summary>
    [ComponentOf(typeof(TaskSpec))]
    public partial class SearchTargetTaskSpec : Entity, IAwake
    {
        public List<EntityRef<AbilitySystemComponent>> _foundTargets = new List<EntityRef<AbilitySystemComponent>>();
    }
}
