using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    /// <summary>
    /// 放置物效果节点 Inspector
    /// </summary>
    public class PlacementEffectNodeInspector : EffectNodeInspector
    {
        protected override bool ShowDurationConfig => true;

        protected override bool ShowPeriodicConfig => true;

        protected override void BuildEffectInspectorUI(VisualElement container, SkillNodeBase node)
        {
            if (node is not PlacementEffectNode placementNode)
            {
                return;
            }

            PlacementEffectNodeData data = placementNode.TypedData;
            if (data == null)
            {
                return;
            }

            BuildPositionSection(container, placementNode, data);
            BuildPlacementSection(container, placementNode, data);
            BuildCollisionSection(container, placementNode, data);
        }

        private void BuildPositionSection(VisualElement container, PlacementEffectNode placementNode, PlacementEffectNodeData data)
        {
            VisualElement positionSection = CreateCollapsibleSection("位置设置", out VisualElement positionContent, true);

            var positionSourceField = new EnumField("位置来源", data.positionSource);
            ApplyEnumFieldStyle(positionSourceField);
            positionSourceField.RegisterValueChangedCallback(evt =>
            {
                data.positionSource = (PositionSourceType)evt.newValue;
                placementNode.SyncUIFromData();
            });
            positionContent.Add(positionSourceField);

            positionContent.Add(CreateTextField("挂点", data.positionBindingName, value =>
            {
                data.positionBindingName = value;
                placementNode.SyncUIFromData();
            }));

            container.Add(positionSection);
        }

        private void BuildPlacementSection(VisualElement container, PlacementEffectNode placementNode, PlacementEffectNodeData data)
        {
            VisualElement placementSection = CreateCollapsibleSection("放置物设置", out VisualElement placementContent, true);

            placementContent.Add(CreateIntField("实体ID", data.placementEntityId, value =>
            {
                data.placementEntityId = value;
                placementNode.SyncUIFromData();
            }));

            var prefabField = new ObjectField("旧Prefab(兼容)")
            {
                objectType = typeof(GameObject),
                value = data.placementPrefab
            };
            prefabField.RegisterValueChangedCallback(evt =>
            {
                data.placementPrefab = evt.newValue as GameObject;
                placementNode.SyncUIFromData();
            });
            placementContent.Add(prefabField);

            container.Add(placementSection);
        }

        private void BuildCollisionSection(VisualElement container, PlacementEffectNode placementNode, PlacementEffectNodeData data)
        {
            VisualElement collisionSection = CreateCollapsibleSection("碰撞设置", out VisualElement collisionContent, true);

            var enableCollisionToggle = new Toggle("启用碰撞检测")
            {
                value = data.enableCollision
            };
            enableCollisionToggle.tooltip = "启用后，放置物会检测范围内的目标并触发进入/离开事件";
            collisionContent.Add(enableCollisionToggle);

            var collisionParamsContainer = new VisualElement();
            collisionParamsContainer.style.marginTop = 4;
            collisionContent.Add(collisionParamsContainer);

            void UpdateCollisionParamsVisibility(bool enableCollision)
            {
                collisionParamsContainer.Clear();
                if (!enableCollision)
                {
                    return;
                }

                collisionParamsContainer.Add(CreateFloatField("碰撞半径", data.collisionRadius, value =>
                {
                    data.collisionRadius = value;
                    placementNode.SyncUIFromData();
                }));

                collisionParamsContainer.Add(CreateTagSetField("碰撞目标标签", data.collisionTargetTags, value =>
                {
                    data.collisionTargetTags = value;
                    placementNode.SyncUIFromData();
                }));

                collisionParamsContainer.Add(CreateTagSetField("碰撞排除标签", data.collisionExcludeTags, value =>
                {
                    data.collisionExcludeTags = value;
                    placementNode.SyncUIFromData();
                }));
            }

            enableCollisionToggle.RegisterValueChangedCallback(evt =>
            {
                data.enableCollision = evt.newValue;
                UpdateCollisionParamsVisibility(evt.newValue);
                placementNode.SyncUIFromData();
            });

            UpdateCollisionParamsVisibility(data.enableCollision);
            container.Add(collisionSection);
        }
    }
}
