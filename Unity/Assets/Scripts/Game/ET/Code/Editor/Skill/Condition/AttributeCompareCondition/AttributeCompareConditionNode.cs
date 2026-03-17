using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class AttributeCompareConditionNode : ConditionNode<AttributeCompareConditionNodeData>
    {
        private AttributeField attrTypeField;
        private EnumField compareOperatorField;
        private EnumField valueTypeField;
        private TextField compareValueField;
        private AttributeField percentageBaseField;
        private VisualElement percentageBaseContainer;

        public AttributeCompareConditionNode(Vector2 position) : base(NodeType.AttributeCompareCondition, position)
        {
        }

        protected override string GetNodeTitle() => "属性比较";
        protected override float GetNodeWidth() => 200;

        protected override void CreateConditionContent()
        {
            attrTypeField = new AttributeField("属性");
            attrTypeField.Value = global::ET.NumericType.Hp;
            attrTypeField.OnValueChanged += value =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.compareAttrType = value;
                NotifyDataChanged();
            };
            mainContainer.Add(attrTypeField);

            compareOperatorField = new EnumField("比较", CompareOperator.Less);
            ApplyFieldStyle(compareOperatorField);
            compareOperatorField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.compareOperator = (CompareOperator)evt.newValue;
                NotifyDataChanged();
            });
            mainContainer.Add(compareOperatorField);

            valueTypeField = new EnumField("值类型", AttributeValueType.Percentage);
            ApplyFieldStyle(valueTypeField);
            valueTypeField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData != null)
                {
                    TypedData.compareValueType = (AttributeValueType)evt.newValue;
                    NotifyDataChanged();
                }

                OnValueTypeChanged((AttributeValueType)evt.newValue);
            });
            mainContainer.Add(valueTypeField);

            compareValueField = CreateFormulaField("比较值", "30", value =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.compareValue = value;
                NotifyDataChanged();
            });
            mainContainer.Add(compareValueField);

            percentageBaseContainer = new VisualElement();
            percentageBaseField = new AttributeField("基准");
            percentageBaseField.Value = global::ET.NumericType.MaxHp;
            percentageBaseField.OnValueChanged += value =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.percentageBaseAttrType = value;
                NotifyDataChanged();
            };
            percentageBaseContainer.Add(percentageBaseField);
            mainContainer.Add(percentageBaseContainer);

            OnValueTypeChanged(AttributeValueType.Percentage);
        }

        private void OnValueTypeChanged(AttributeValueType valueType)
        {
            percentageBaseContainer.style.display = valueType == AttributeValueType.Percentage
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        protected override void SyncConditionContentFromData()
        {
            if (TypedData == null)
            {
                return;
            }

            if (attrTypeField != null)
            {
                attrTypeField.Value = TypedData.compareAttrType;
            }

            if (compareOperatorField != null)
            {
                compareOperatorField.SetValueWithoutNotify(TypedData.compareOperator);
            }

            if (valueTypeField != null)
            {
                valueTypeField.SetValueWithoutNotify(TypedData.compareValueType);
                OnValueTypeChanged(TypedData.compareValueType);
            }

            if (compareValueField != null)
            {
                compareValueField.SetValueWithoutNotify(TypedData.compareValue ?? "30");
            }

            if (percentageBaseField != null)
            {
                percentageBaseField.Value = TypedData.percentageBaseAttrType;
            }
        }
    }
}
