using System.Collections.Generic;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 搜索配置
    /// </summary>
    public struct SearchConfig
    {
        /// <summary>
        /// 搜索中心点
        /// </summary>
        public Vector3 Center;

        /// <summary>
        /// 搜索方向
        /// </summary>
        public Vector3 Direction;

        /// <summary>
        /// 搜索者（用于排除自己）
        /// </summary>
        public AbilitySystemComponent Searcher;

        /// <summary>
        /// 目标标签（必须拥有）
        /// </summary>
        public GameplayTagSet TargetTags;

        /// <summary>
        /// 排除标签（不能拥有）
        /// </summary>
        public GameplayTagSet ExcludeTags;

        /// <summary>
        /// 最大目标数（0表示无限制）
        /// </summary>
        public int MaxTargets;

        /// <summary>
        /// 物理层掩码
        /// </summary>
        public int LayerMask;
    }
    /// <summary>
    /// 目标搜索器 - 提供各种形状的目标搜索功能
    /// </summary>
    public partial class TargetSearcher : Entity ,IAwake 
    {
        /// <summary>
        /// 默认物理层掩码
        /// </summary>
        public const int DefaultLayerMask = -1;
    }
}
