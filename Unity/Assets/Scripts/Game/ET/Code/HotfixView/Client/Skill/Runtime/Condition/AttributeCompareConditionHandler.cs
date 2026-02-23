using Unity.Mathematics;

namespace ET.Client
{
    /// <summary>
    /// 属性比较条件Handler - 参考 AIDispatcherComponent 的 Handler 模式
    /// </summary> 
    [FriendOf(typeof(AbilitySystemComponent))]

    public class AttributeCompareConditionHandler : AConditionHandler
    {
        public override bool Evaluate(AbilitySystemComponent target)
        {
            var nodeData = ConditionSpec.NodeData as AttributeCompareConditionNodeData;

            if (target?.Attributes == null || nodeData == null)
                return false;

            float? attrValue = target.Attributes.GetCurrentValue(nodeData.compareAttrType);
            if (!attrValue.HasValue)
                return false;

            float compareValue = FormulaEvaluator.EvaluateSimple(nodeData.compareValue, 0f);
            if (nodeData.compareValueType == AttributeValueType.Percentage)
            {
                float? baseValue = target.Attributes.GetCurrentValue(nodeData.percentageBaseAttrType);
                if (baseValue.HasValue)
                    compareValue = baseValue.Value * (compareValue / 100f);
            }

            bool ret = false;
            switch (nodeData.compareOperator)
            {
                case CompareOperator.Equal:
                    ret = math.abs(attrValue.Value - compareValue) < 0.0001f;
                    break;
                case CompareOperator.NotEqual:
                    ret = math.abs(attrValue.Value - compareValue) >= 0.0001f;
                    break;
                case CompareOperator.Greater:
                    ret = attrValue.Value > compareValue;
                    break;
                case CompareOperator.GreaterOrEqual:
                    ret = attrValue.Value >= compareValue;
                    break;
                case CompareOperator.Less:
                    ret = attrValue.Value < compareValue;
                    break;
                case CompareOperator.LessOrEqual:
                    ret = attrValue.Value <= compareValue;
                    break;
            }
            return ret;
        }
    }
}
