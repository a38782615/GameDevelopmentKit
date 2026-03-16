using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class DamageEffectNodeInspector : EffectNodeInspector
    {
        protected override bool ShowAttributeModifiers => true;

        protected override void BuildEffectInspectorUI(VisualElement container, SkillNodeBase node)
        {
            if (node is not DamageEffectNode damageNode)
            {
                return;
            }

            var data = damageNode.TypedData;
            if (data == null)
            {
                return;
            }

            var damageTypeField = new EnumField("伤害类型", data.damageType) { style = { marginBottom = 8 } };
            ApplyEnumFieldStyle(damageTypeField);
            damageTypeField.RegisterValueChangedCallback(evt =>
            {
                data.damageType = (DamageType)evt.newValue;
                damageNode.SyncUIFromData();
            });
            container.Add(damageTypeField);

            container.Add(CreateMagnitudeSourceUIWithMMCDetail("伤害值", data, damageNode));

            var calcTypeField = new EnumField("计算方式", data.damageCalculationType) { style = { marginTop = 4 } };
            ApplyEnumFieldStyle(calcTypeField);
            calcTypeField.RegisterValueChangedCallback(evt =>
            {
                data.damageCalculationType = (DamageCalculationType)evt.newValue;
                damageNode.SyncUIFromData();
            });
            container.Add(calcTypeField);

            container.Add(CreateKnockbackSection(data, damageNode));
        }

        private VisualElement CreateMagnitudeSourceUIWithMMCDetail(string label, DamageEffectNodeData data, DamageEffectNode node)
        {
            var container = new VisualElement();
            container.style.marginBottom = 8;

            var labelElement = new Label(label);
            labelElement.style.marginBottom = 4;
            container.Add(labelElement);

            var valueRow = new VisualElement();
            valueRow.style.flexDirection = FlexDirection.Row;

            var sourceTypeField = new EnumField(data.damageSourceType);
            sourceTypeField.style.width = 100;
            sourceTypeField.style.marginRight = 4;
            ApplyEnumFieldStyle(sourceTypeField);
            valueRow.Add(sourceTypeField);

            var fixedValueField = new FloatField { value = data.damageFixedValue };
            fixedValueField.style.flexGrow = 1;
            fixedValueField.style.display = data.damageSourceType == ModifierMagnitudeSourceType.FixedValue
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            fixedValueField.RegisterValueChangedCallback(evt =>
            {
                data.damageFixedValue = evt.newValue;
                node.SyncUIFromData();
            });
            valueRow.Add(fixedValueField);

            var formulaField = new TextField { value = data.damageFormula ?? string.Empty };
            formulaField.style.flexGrow = 1;
            formulaField.style.display = data.damageSourceType == ModifierMagnitudeSourceType.Formula
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            formulaField.RegisterValueChangedCallback(evt =>
            {
                data.damageFormula = evt.newValue;
                node.SyncUIFromData();
            });
            valueRow.Add(formulaField);

            var mmcTypeField = new EnumField(data.damageMMCType);
            mmcTypeField.style.flexGrow = 1;
            mmcTypeField.style.display = data.damageSourceType == ModifierMagnitudeSourceType.ModifierMagnitudeCalculation
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            ApplyEnumFieldStyle(mmcTypeField);
            valueRow.Add(mmcTypeField);

            var setByCallerField = new TextField { value = data.damageSetByCallerKey ?? string.Empty };
            setByCallerField.style.flexGrow = 1;
            setByCallerField.style.display = data.damageSourceType == ModifierMagnitudeSourceType.SetByCaller
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            setByCallerField.RegisterValueChangedCallback(evt =>
            {
                data.damageSetByCallerKey = evt.newValue;
                node.SyncUIFromData();
            });
            valueRow.Add(setByCallerField);

            container.Add(valueRow);

            var mmcDetailContainer = new VisualElement();
            mmcDetailContainer.style.marginTop = 4;
            mmcDetailContainer.style.marginLeft = 8;
            mmcDetailContainer.style.paddingLeft = 8;
            mmcDetailContainer.style.borderLeftWidth = 2;
            mmcDetailContainer.style.borderLeftColor = new Color(0.3f, 0.6f, 0.9f);
            mmcDetailContainer.style.display = data.damageSourceType == ModifierMagnitudeSourceType.ModifierMagnitudeCalculation
                && data.damageMMCType == MMCType.AttributeBased
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            var mmcCaptureAttrField = new AttributeField("捕获属性");
            mmcCaptureAttrField.Value = data.damageMMCCaptureAttribute;
            mmcCaptureAttrField.OnValueChanged += value =>
            {
                data.damageMMCCaptureAttribute = value;
                node.SyncUIFromData();
            };
            mmcDetailContainer.Add(mmcCaptureAttrField);

            var mmcSourceField = new EnumField("属性来源", data.damageMMCAttributeSource);
            mmcSourceField.style.marginBottom = 4;
            ApplyEnumFieldStyle(mmcSourceField);
            mmcSourceField.RegisterValueChangedCallback(evt =>
            {
                data.damageMMCAttributeSource = (MMCAttributeSource)evt.newValue;
                node.SyncUIFromData();
            });
            mmcDetailContainer.Add(mmcSourceField);

            var mmcCoefficientField = new FloatField("系数") { value = data.damageMMCCoefficient };
            mmcCoefficientField.style.marginBottom = 4;
            mmcCoefficientField.RegisterValueChangedCallback(evt =>
            {
                data.damageMMCCoefficient = evt.newValue;
                node.SyncUIFromData();
            });
            mmcDetailContainer.Add(mmcCoefficientField);

            var mmcSnapshotToggle = new Toggle("使用快照") { value = data.damageMMCUseSnapshot };
            mmcSnapshotToggle.tooltip = "开启后在施放时捕获属性值，后续不再实时读取。";
            mmcSnapshotToggle.RegisterValueChangedCallback(evt =>
            {
                data.damageMMCUseSnapshot = evt.newValue;
                node.SyncUIFromData();
            });
            mmcDetailContainer.Add(mmcSnapshotToggle);

            container.Add(mmcDetailContainer);

            var stackMultiplyToggle = new Toggle("乘以堆叠层数") { value = data.damageMultiplyByStackCount };
            stackMultiplyToggle.tooltip = "开启后，来自 Buff 周期触发的伤害会乘以 Buff 当前层数。";
            stackMultiplyToggle.style.marginTop = 8;
            stackMultiplyToggle.RegisterValueChangedCallback(evt =>
            {
                data.damageMultiplyByStackCount = evt.newValue;
                node.SyncUIFromData();
            });
            container.Add(stackMultiplyToggle);

            sourceTypeField.RegisterValueChangedCallback(evt =>
            {
                var newType = (ModifierMagnitudeSourceType)evt.newValue;
                data.damageSourceType = newType;

                fixedValueField.style.display = newType == ModifierMagnitudeSourceType.FixedValue
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                formulaField.style.display = newType == ModifierMagnitudeSourceType.Formula
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                mmcTypeField.style.display = newType == ModifierMagnitudeSourceType.ModifierMagnitudeCalculation
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                setByCallerField.style.display = newType == ModifierMagnitudeSourceType.SetByCaller
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                mmcDetailContainer.style.display = newType == ModifierMagnitudeSourceType.ModifierMagnitudeCalculation
                    && data.damageMMCType == MMCType.AttributeBased
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;

                node.SyncUIFromData();
            });

            mmcTypeField.RegisterValueChangedCallback(evt =>
            {
                data.damageMMCType = (MMCType)evt.newValue;
                mmcDetailContainer.style.display = data.damageSourceType == ModifierMagnitudeSourceType.ModifierMagnitudeCalculation
                    && data.damageMMCType == MMCType.AttributeBased
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
                node.SyncUIFromData();
            });

            return container;
        }

        private VisualElement CreateKnockbackSection(DamageEffectNodeData data, DamageEffectNode node)
        {
            var section = CreateCollapsibleSection("受击击退", out var content, true);

            var enableToggle = new Toggle("启用受击击退") { value = data.enableHitKnockback };
            enableToggle.style.marginBottom = 4;
            content.Add(enableToggle);

            var paramsContainer = new VisualElement();
            content.Add(paramsContainer);

            void Refresh(bool enabled)
            {
                paramsContainer.Clear();
                if (!enabled)
                {
                    return;
                }

                paramsContainer.Add(CreateFloatField("击退距离", data.knockbackDistance, value =>
                {
                    data.knockbackDistance = value;
                    node.SyncUIFromData();
                }));

                paramsContainer.Add(CreateFloatField("击退速度", data.knockbackSpeed, value =>
                {
                    data.knockbackSpeed = value;
                    node.SyncUIFromData();
                }));

                var hint = new Label("持续时间会自动按 击退距离 / 击退速度 计算。");
                hint.style.marginTop = 4;
                hint.style.color = new Color(0.7f, 0.7f, 0.7f);
                hint.style.whiteSpace = WhiteSpace.Normal;
                paramsContainer.Add(hint);
            }

            enableToggle.RegisterValueChangedCallback(evt =>
            {
                data.enableHitKnockback = evt.newValue;
                Refresh(evt.newValue);
                node.SyncUIFromData();
            });

            Refresh(data.enableHitKnockback);
            return section;
        }
    }
}
