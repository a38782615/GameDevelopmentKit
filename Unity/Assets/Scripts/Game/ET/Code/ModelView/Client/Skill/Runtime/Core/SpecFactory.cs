namespace ET.Client
{
    /// <summary>
    /// Spec工厂 - 适配 ET Entity 架构
    /// Effect 和 Cue 通过 AddChild 创建 Entity
    /// Task 保持原有方式（瞬时执行，不需要 Entity）
    /// Condition 改由 ConditionDispatcher 处理，不再在此创建
    /// </summary>
    public static class SpecFactory
    {
        /// <summary>
        /// 创建任务Spec（保持原有方式，瞬时执行不需要Entity）
        /// </summary>
        public static TaskSpec CreateTaskSpec(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.SearchTargetTask:
                    return new SearchTargetTaskSpec();
                case NodeType.EndAbilityTask:
                    return new EndAbilityTaskSpec();
                default:
                    return null;
            }
        }

        /// <summary>
        /// 判断节点类型是否为瞬时效果
        /// </summary>
        public static bool IsInstantEffect(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.DamageEffect:
                case NodeType.HealEffect:
                case NodeType.CostEffect:
                case NodeType.ModifyAttributeEffect:
                case NodeType.ProjectileEffect:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断节点类型是否为持续效果
        /// </summary>
        public static bool IsDurationEffect(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.CooldownEffect:
                case NodeType.BuffEffect:
                case NodeType.PlacementEffect:
                case NodeType.DisplaceEffect:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断节点类型是否为任务节点
        /// </summary>
        public static bool IsTaskNode(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.SearchTargetTask:
                case NodeType.EndAbilityTask:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 判断节点类型是否为条件节点
        /// </summary>
        public static bool IsConditionNode(NodeType nodeType)
        {
            switch (nodeType)
            {
                case NodeType.AttributeCompareCondition:
                    return true;
                default:
                    return false;
            }
        }
    }
}
