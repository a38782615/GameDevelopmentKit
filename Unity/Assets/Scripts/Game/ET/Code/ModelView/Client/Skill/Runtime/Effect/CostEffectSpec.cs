

namespace ET.Client
{
    /// <summary>
    /// 消耗效果Spec（瞬时效果）
    /// </summary>
    [EnableClass]
    public class CostEffectSpec : GameplayEffectSpec
    {
        private CostEffectNodeData CostNodeData => NodeData as CostEffectNodeData;
    }
}
