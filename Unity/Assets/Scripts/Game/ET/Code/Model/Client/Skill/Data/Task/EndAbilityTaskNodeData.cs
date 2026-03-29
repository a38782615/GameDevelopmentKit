using System;

namespace ET.Client
{
    /// <summary>
    /// 结束技能任务节点数据
    /// 用于结束当前技能的执行
    /// </summary>
    [Serializable]
    [EnableClass]
    public class EndAbilityTaskNodeData : TaskNodeData
    {
        /// <summary>
        /// 结束类型：正常结束或取消
        /// </summary>
        public EndAbilityType endType = EndAbilityType.Normal;
    }
}
