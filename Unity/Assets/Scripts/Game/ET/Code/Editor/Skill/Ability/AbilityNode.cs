using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class AbilityNode : SkillNodeBase<AbilityNodeData>
    {
        private Port activatePort;
        private Port animationPort;
        private Port costPort;
        private Port cooldownPort;
        private readonly List<Port> eventOutputPorts = new List<Port>();
        private VisualElement eventPortsContainer;

        public AbilityNode(Vector2 position) : base(NodeType.Ability, position)
        {
        }

        protected override string GetNodeTitle() => "技能";

        protected override float GetNodeWidth() => 310f;

        protected override bool HasDefaultInputPort => false;

        protected override void CreateContent()
        {
            this.activatePort = CreateOutputPort(SkillPortId.Ability.Activate);
            this.animationPort = CreateOutputPort(SkillPortId.Ability.Animation);

            this.costPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            ConfigureOutputPort(this.costPort, SkillPortId.Ability.Cost);
            inputContainer.Add(this.costPort);

            this.cooldownPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            ConfigureOutputPort(this.cooldownPort, SkillPortId.Ability.Cooldown);
            inputContainer.Add(this.cooldownPort);

            CreateEventPortsSection();
        }

        private void UpdateTitle()
        {
            int skillId = TypedData?.skillId ?? 0;
            title = skillId > 0 ? $"技能[{skillId}]" : "技能";
        }

        private void CreateEventPortsSection()
        {
            this.eventPortsContainer = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    marginTop = 8
                }
            };

            var headerContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.SpaceBetween,
                    alignItems = Align.Center,
                    marginBottom = 8
                }
            };

            var titleLabel = new Label("事件监听")
            {
                style =
                {
                    fontSize = 12,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white
                }
            };

            var addButton = new Button { text = "+" };
            addButton.style.width = 24;
            addButton.style.height = 24;
            ApplyButtonStyle(addButton);
            addButton.clicked += () => AddEventOutputPort();

            headerContainer.Add(titleLabel);
            headerContainer.Add(addButton);
            this.eventPortsContainer.Add(headerContainer);
            this.eventPortsContainer.Add(new VisualElement { name = "EventPortsListContainer" });
            outputContainer.Add(this.eventPortsContainer);
        }

        private void AddEventOutputPort(AbilityEventPortData eventData = null)
        {
            if (TypedData == null)
            {
                return;
            }

            TypedData.eventOutputPorts ??= new List<AbilityEventPortData>();
            if (eventData == null)
            {
                eventData = new AbilityEventPortData();
                TypedData.eventOutputPorts.Add(eventData);
                NotifyDataChanged();
            }

            int index = TypedData.eventOutputPorts.IndexOf(eventData);
            if (index < 0)
            {
                index = TypedData.eventOutputPorts.Count - 1;
            }

            eventData.PortId = SkillPortIdUtility.ResolveAbilityEventPortId(eventData.eventType);

            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            ConfigureOutputPort(port, eventData.PortId, GetEventPortName(eventData));
            port.portColor = new Color(0.3f, 0.7f, 0.9f);

            var portRowContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 4
                }
            };

            var eventTypeField = new EnumField(eventData.eventType);
            eventTypeField.style.width = 100;
            eventTypeField.style.marginRight = 4;
            ApplyFieldStyle(eventTypeField);

            var customTagField = new TextField { value = eventData.customEventTag ?? string.Empty };
            customTagField.style.width = 60;
            customTagField.style.marginRight = 4;
            customTagField.style.display = DisplayStyle.Flex;
            ApplyFieldStyle(customTagField);

            var deleteButton = new Button { text = "X" };
            deleteButton.style.width = 20;
            deleteButton.style.height = 20;
            ApplyButtonStyle(deleteButton);

            int currentIndex = index;
            eventTypeField.RegisterValueChangedCallback(evt =>
            {
                GameplayEventType newType = (GameplayEventType)evt.newValue;
                if (TypedData == null || currentIndex >= TypedData.eventOutputPorts.Count)
                {
                    return;
                }

                AbilityEventPortData currentData = TypedData.eventOutputPorts[currentIndex];
                currentData.eventType = newType;
                currentData.PortId = SkillPortIdUtility.ResolveAbilityEventPortId(newType);
                ConfigureOutputPort(port, currentData.PortId, GetEventPortName(currentData));
                customTagField.style.display = DisplayStyle.Flex;
                NotifyDataChanged();
            });

            customTagField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null || currentIndex >= TypedData.eventOutputPorts.Count)
                {
                    return;
                }

                AbilityEventPortData currentData = TypedData.eventOutputPorts[currentIndex];
                currentData.customEventTag = evt.newValue;
                currentData.PortId = SkillPortIdUtility.ResolveAbilityEventPortId(currentData.eventType);
                ConfigureOutputPort(port, currentData.PortId, GetEventPortName(currentData));
                NotifyDataChanged();
            });

            deleteButton.clicked += () => RemoveEventOutputPort(currentIndex, port, portRowContainer);

            portRowContainer.Add(eventTypeField);
            portRowContainer.Add(customTagField);
            portRowContainer.Add(port);
            portRowContainer.Add(deleteButton);

            VisualElement portsListContainer = this.eventPortsContainer.Q("EventPortsListContainer");
            portsListContainer?.Add(portRowContainer);

            this.eventOutputPorts.Add(port);
            RefreshPorts();
        }

        private void RemoveEventOutputPort(int index, Port port, VisualElement portRowContainer)
        {
            if (TypedData == null || TypedData.eventOutputPorts == null)
            {
                return;
            }

            if (index < 0 || index >= TypedData.eventOutputPorts.Count)
            {
                return;
            }

            TypedData.eventOutputPorts.RemoveAt(index);
            this.eventOutputPorts.Remove(port);
            portRowContainer.RemoveFromHierarchy();
            NotifyDataChanged();
            RefreshEventPortsList();
        }

        private void RefreshEventPortsList()
        {
            foreach (Port port in this.eventOutputPorts)
            {
                port.RemoveFromHierarchy();
            }

            this.eventOutputPorts.Clear();
            VisualElement portsListContainer = this.eventPortsContainer?.Q("EventPortsListContainer");
            portsListContainer?.Clear();

            if (TypedData == null)
            {
                return;
            }

            TypedData.eventOutputPorts ??= new List<AbilityEventPortData>();
            foreach (AbilityEventPortData eventData in TypedData.eventOutputPorts)
            {
                AddEventOutputPort(eventData);
            }
        }

        private string GetEventPortName(AbilityEventPortData eventData)
        {
            switch (eventData.eventType)
            {
                case GameplayEventType.OnHit:
                    return GetOutputPortName(SkillPortId.Ability.EventOnKill);
                case GameplayEventType.OnDealDamage:
                    return GetOutputPortName(SkillPortId.Ability.EventOnDealDamage);
                case GameplayEventType.OnTakeDamage:
                    return GetOutputPortName(SkillPortId.Ability.EventOnTakeDamage);
                case GameplayEventType.OnDeath:
                    return GetOutputPortName(SkillPortId.Ability.EventOnDeath);
                case GameplayEventType.OnKill:
                    return GetOutputPortName(SkillPortId.Ability.EventOnKill);
                default:
                    return GetOutputPortName(SkillPortId.Ability.EventOnKill);
            }
        }

        private void ApplyButtonStyle(Button button)
        {
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.borderTopLeftRadius = 4;
            button.style.borderTopRightRadius = 4;
            button.style.borderBottomLeftRadius = 4;
            button.style.borderBottomRightRadius = 4;
        }

        public override Port FindOutputPortByIdentifier(int portId)
        {
            if (SkillNodeBase.GetPortId(this.activatePort) == portId) return this.activatePort;
            if (SkillNodeBase.GetPortId(this.costPort) == portId) return this.costPort;
            if (SkillNodeBase.GetPortId(this.cooldownPort) == portId) return this.cooldownPort;
            if (SkillNodeBase.GetPortId(this.animationPort) == portId) return this.animationPort;

            foreach (Port port in this.eventOutputPorts)
            {
                if (SkillNodeBase.GetPortId(port) == portId)
                {
                    return port;
                }
            }

            return base.FindOutputPortByIdentifier(portId);
        }

        public override void LoadData(NodeData data)
        {
            base.LoadData(data);
            UpdateTitle();
            SyncUIFromData();
        }

        public override void SyncUIFromData()
        {
            base.SyncUIFromData();
            if (TypedData == null)
            {
                return;
            }

            UpdateTitle();
            RefreshEventPortsList();
        }
    }
}
