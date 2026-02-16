

namespace ET.Client
{
    /// <summary>
    /// 结束技能任务Spec
    /// </summary>
    [EnableClass]
    public class EndAbilityTaskSpec : TaskSpec
    {
        private EndAbilityTaskNodeData EndAbilityNodeData => NodeData as EndAbilityTaskNodeData;

        protected override void OnExecute(AbilitySystemComponent target)
        {
            var nodeData = EndAbilityNodeData;
            bool endAsCancelled = nodeData?.endType == EndAbilityType.Cancel;
            Context?.AbilitySpec?.End(endAsCancelled);
        }
    }
}
