using System.Collections.Generic;

namespace ET.Client
{
    /// <summary>
    /// 公式计算上下文
    /// </summary>
    public class FormulaContext : Object
    {
        /// <summary>
        /// 施法者属性容器
        /// </summary>
        public AttributeSetContainer CasterAttributes { get; set; }

        /// <summary>
        /// 目标属性容器
        /// </summary>
        public AttributeSetContainer TargetAttributes { get; set; }

        /// <summary>
        /// 自定义变量
        /// </summary>
        public Dictionary<string, float> Variables { get; set; }

        /// <summary>
        /// 堆叠层数
        /// </summary>
        public int StackCount { get; set; } = 1;

        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; set; } = 1;

        /// <summary>
        /// 从执行上下文创建
        /// </summary>
        public static FormulaContext FromExecutionContext(
            SpecExecutionContext execContext,
            AbilitySystemComponent target = null)
        {
            return new FormulaContext
            {
                CasterAttributes = execContext.Caster?.Attributes,
                TargetAttributes = target?.Attributes ?? execContext.MainTarget?.Attributes,
                Level = execContext.AbilityLevel,
                StackCount = 1
            };
        }
    }
}
