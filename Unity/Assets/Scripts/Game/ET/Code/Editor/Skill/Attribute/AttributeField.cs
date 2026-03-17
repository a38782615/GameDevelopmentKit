using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class AttributeField : VisualElement
    {
        private readonly List<int> numericTypes = new List<int>(global::ET.NumericType.GetClientAttributeTypes());
        private readonly List<string> displayNames = new List<string>();
        private readonly Label labelElement;
        private readonly PopupField<string> popupField;
        private int currentValue;

        public event Action<int> OnValueChanged;

        public int Value
        {
            get => currentValue;
            set
            {
                currentValue = value;
                RefreshSelection();
            }
        }

        public AttributeField(string label = "属性")
        {
            for (int i = 0; i < numericTypes.Count; i++)
            {
                displayNames.Add(global::ET.NumericType.GetAttributeName(numericTypes[i]));
            }

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;
            style.marginBottom = 4;

            labelElement = new Label(label)
            {
                style =
                {
                    width = 100,
                    minWidth = 100
                }
            };
            Add(labelElement);

            popupField = new PopupField<string>(displayNames, 0)
            {
                style =
                {
                    flexGrow = 1
                }
            };
            popupField.RegisterValueChangedCallback(_ =>
            {
                int index = popupField.index;
                if (index < 0 || index >= numericTypes.Count)
                {
                    return;
                }

                currentValue = numericTypes[index];
                OnValueChanged?.Invoke(currentValue);
            });
            Add(popupField);
            RefreshSelection();
        }

        public void SetLabel(string label)
        {
            labelElement.text = label;
        }

        private void RefreshSelection()
        {
            int index = numericTypes.IndexOf(currentValue);
            if (index < 0)
            {
                index = 0;
                currentValue = numericTypes.Count > 0 ? numericTypes[0] : global::ET.NumericType.None;
            }

            if (displayNames.Count > 0)
            {
                popupField.index = index;
            }
        }
    }

    public class AttributeModifierField : VisualElement
    {
        private readonly AttributeModifierData data;
        private readonly AttributeField attributeField;
        private readonly EnumField operationField;
        private readonly VisualElement valueContainer;
        private readonly EnumField sourceTypeField;
        private readonly FloatField fixedValueField;
        private readonly TextField formulaField;
        private readonly EnumField mmcTypeField;
        private readonly TextField setByCallerField;

        public event Action OnDataChanged;

        public AttributeModifierField(AttributeModifierData modifierData)
        {
            data = modifierData;

            style.backgroundColor = new Color(45f / 255f, 45f / 255f, 45f / 255f);
            style.borderTopLeftRadius = 4;
            style.borderTopRightRadius = 4;
            style.borderBottomLeftRadius = 4;
            style.borderBottomRightRadius = 4;
            style.paddingLeft = 6;
            style.paddingRight = 6;
            style.paddingTop = 6;
            style.paddingBottom = 6;
            style.marginBottom = 4;

            attributeField = new AttributeField("目标属性");
            attributeField.Value = data.attrType;
            attributeField.OnValueChanged += value =>
            {
                data.attrType = value;
                OnDataChanged?.Invoke();
            };
            Add(attributeField);

            operationField = new EnumField("操作", data.operation);
            operationField.style.marginBottom = 4;
            operationField.RegisterValueChangedCallback(evt =>
            {
                data.operation = (ModifierOperation)evt.newValue;
                OnDataChanged?.Invoke();
            });
            Add(operationField);

            valueContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            valueContainer.Add(new Label("数值")
            {
                style = { width = 100, minWidth = 100 }
            });

            fixedValueField = new FloatField
            {
                value = data.fixedValue,
                style = { flexGrow = 1 }
            };
            fixedValueField.RegisterValueChangedCallback(evt =>
            {
                data.fixedValue = evt.newValue;
                OnDataChanged?.Invoke();
            });

            formulaField = new TextField
            {
                value = data.formula ?? string.Empty,
                style = { flexGrow = 1 }
            };
            formulaField.RegisterValueChangedCallback(evt =>
            {
                data.formula = evt.newValue;
                OnDataChanged?.Invoke();
            });

            mmcTypeField = new EnumField(data.mmcType)
            {
                style = { flexGrow = 1 }
            };
            mmcTypeField.RegisterValueChangedCallback(evt =>
            {
                data.mmcType = (MMCType)evt.newValue;
                OnDataChanged?.Invoke();
            });

            setByCallerField = new TextField
            {
                value = data.setByCallerKey ?? string.Empty,
                style = { flexGrow = 1 }
            };
            setByCallerField.RegisterValueChangedCallback(evt =>
            {
                data.setByCallerKey = evt.newValue;
                OnDataChanged?.Invoke();
            });

            sourceTypeField = new EnumField(data.magnitudeSourceType)
            {
                style = { width = 120, marginLeft = 4 }
            };
            sourceTypeField.RegisterValueChangedCallback(evt =>
            {
                data.magnitudeSourceType = (ModifierMagnitudeSourceType)evt.newValue;
                RefreshValueInput();
                OnDataChanged?.Invoke();
            });

            Add(valueContainer);
            RefreshValueInput();
        }

        private void RefreshValueInput()
        {
            while (valueContainer.childCount > 1)
            {
                valueContainer.RemoveAt(1);
            }

            switch (data.magnitudeSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    fixedValueField.SetValueWithoutNotify(data.fixedValue);
                    valueContainer.Add(fixedValueField);
                    break;
                case ModifierMagnitudeSourceType.Formula:
                    formulaField.SetValueWithoutNotify(data.formula ?? string.Empty);
                    valueContainer.Add(formulaField);
                    break;
                case ModifierMagnitudeSourceType.ModifierMagnitudeCalculation:
                    mmcTypeField.SetValueWithoutNotify(data.mmcType);
                    valueContainer.Add(mmcTypeField);
                    break;
                case ModifierMagnitudeSourceType.SetByCaller:
                    setByCallerField.SetValueWithoutNotify(data.setByCallerKey ?? string.Empty);
                    valueContainer.Add(setByCallerField);
                    break;
            }

            valueContainer.Add(sourceTypeField);
        }

        public void Refresh()
        {
            attributeField.Value = data.attrType;
            operationField.SetValueWithoutNotify(data.operation);
            sourceTypeField.SetValueWithoutNotify(data.magnitudeSourceType);
            RefreshValueInput();
        }
    }

    public class AttributeModifierListField : VisualElement
    {
        private readonly List<AttributeModifierData> dataList;
        private readonly VisualElement listContainer;

        public event Action OnDataChanged;

        public AttributeModifierListField(List<AttributeModifierData> modifiers, string title = "属性修改器")
        {
            dataList = modifiers;

            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            header.Add(new Label(title)
            {
                style = { unityFontStyleAndWeight = FontStyle.Bold }
            });

            var addButton = new Button(AddModifier)
            {
                text = "+",
                style = { width = 24 }
            };
            header.Add(addButton);

            Add(header);

            listContainer = new VisualElement();
            Add(listContainer);

            RefreshList();
        }

        private void RefreshList()
        {
            listContainer.Clear();

            for (int i = 0; i < dataList.Count; i++)
            {
                int index = i;
                AttributeModifierData modifierData = dataList[index];

                var itemContainer = new VisualElement();

                var modifierField = new AttributeModifierField(modifierData);
                modifierField.OnDataChanged += () => OnDataChanged?.Invoke();
                itemContainer.Add(modifierField);

                var deleteButton = new Button(() =>
                {
                    dataList.RemoveAt(index);
                    RefreshList();
                    OnDataChanged?.Invoke();
                })
                {
                    text = "删除",
                    style = { marginBottom = 8 }
                };
                itemContainer.Add(deleteButton);

                listContainer.Add(itemContainer);
            }

            if (dataList.Count == 0)
            {
                listContainer.Add(new Label("暂无修改器，点击 + 添加")
                {
                    style = { color = new Color(0.5f, 0.5f, 0.5f) }
                });
            }
        }

        private void AddModifier()
        {
            dataList.Add(new AttributeModifierData());
            RefreshList();
            OnDataChanged?.Invoke();
        }
    }
}
