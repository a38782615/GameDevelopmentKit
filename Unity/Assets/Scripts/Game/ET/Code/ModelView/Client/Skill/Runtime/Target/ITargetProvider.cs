using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 目标提供者接口
    /// 用于抽象不同的目标获取方式
    /// </summary>
    public interface ITargetProvider
    {
        /// <summary>
        /// 获取单个目标
        /// </summary>
        AbilitySystemComponent GetTarget();

        /// <summary>
        /// 获取所有目标
        /// </summary>
        List<AbilitySystemComponent> GetTargets();

        /// <summary>
        /// 目标数量
        /// </summary>
        int TargetCount { get; }

        /// <summary>
        /// 是否有有效目标
        /// </summary>
        bool HasValidTargets { get; }

        /// <summary>
        /// 清除目标
        /// </summary>
        void Clear();
    }
}
