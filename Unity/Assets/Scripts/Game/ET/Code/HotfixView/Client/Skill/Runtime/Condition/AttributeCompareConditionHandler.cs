using Unity.Mathematics;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(ConditionSpec))]
    public class AttributeCompareConditionHandler : AConditionHandler
    {
        public override SpecExecutionContext GetContext()
        {
            return this.Spec?.GetContext();
        }

        public override bool Evaluate(AbilitySystemComponent target)
        {
            AttributeCompareConditionNodeData nodeData = this.NodeData as AttributeCompareConditionNodeData;
            if (target?.Attributes == null || nodeData == null)
            {
                return false;
            }

            float? attrValue = target.Attributes.GetCurrentValue(nodeData.compareAttrType);
            if (!attrValue.HasValue)
            {
                return false;
            }

            float compareValue = FormulaEvaluator.EvaluateSimple(nodeData.compareValue, 0f);
            if (nodeData.compareValueType == AttributeValueType.Percentage)
            {
                float? baseValue = target.Attributes.GetCurrentValue(nodeData.percentageBaseAttrType);
                if (baseValue.HasValue)
                {
                    compareValue = baseValue.Value * (compareValue / 100f);
                }
            }

            switch (nodeData.compareOperator)
            {
                case CompareOperator.Equal:
                    return math.abs(attrValue.Value - compareValue) < 0.0001f;
                case CompareOperator.NotEqual:
                    return math.abs(attrValue.Value - compareValue) >= 0.0001f;
                case CompareOperator.Greater:
                    return attrValue.Value > compareValue;
                case CompareOperator.GreaterOrEqual:
                    return attrValue.Value >= compareValue;
                case CompareOperator.Less:
                    return attrValue.Value < compareValue;
                case CompareOperator.LessOrEqual:
                    return attrValue.Value <= compareValue;
                default:
                    return false;
            }
        }
    }
}
