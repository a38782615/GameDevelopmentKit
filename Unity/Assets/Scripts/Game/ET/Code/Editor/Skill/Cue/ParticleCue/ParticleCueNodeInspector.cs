using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public class ParticleCueNodeInspector : CueNodeInspector
    {
        protected override void BuildCueInspectorUI(VisualElement container, SkillNodeBase node)
        {
            if (node is not ParticleCueNode particleCueNode)
            {
                return;
            }

            ParticleCueNodeData data = particleCueNode.TypedData;
            if (data == null)
            {
                return;
            }

            var positionSection = CreateCollapsibleSection("位置设置", out var positionContent, true);

            var positionSourceField = new EnumField("位置来源", data.positionSource);
            ApplyEnumFieldStyle(positionSourceField);
            positionSourceField.RegisterValueChangedCallback(evt =>
            {
                data.positionSource = (PositionSourceType)evt.newValue;
                particleCueNode.SyncUIFromData();
            });
            positionContent.Add(positionSourceField);

            positionContent.Add(CreateTextField("挂点", data.particleBindingName ?? string.Empty, value =>
            {
                data.particleBindingName = value;
                particleCueNode.SyncUIFromData();
            }));

            container.Add(positionSection);

            var particleSection = CreateCollapsibleSection("粒子设置", out var particleContent, true);

            particleContent.Add(CreateIntField("实体ID", data.particleEntityId, value =>
            {
                data.particleEntityId = value;
                particleCueNode.SyncUIFromData();
                particleCueNode.NotifyConnectedTracksUpdateDuration();
            }));

            var offsetContainer = new VisualElement { style = { marginBottom = 8 } };
            offsetContainer.Add(new Label("偏移") { style = { marginBottom = 4 } });
            offsetContainer.Add(CreateFloatField("X", data.particleOffset.x, value =>
            {
                data.particleOffset = new Vector3(value, data.particleOffset.y, data.particleOffset.z);
                particleCueNode.SyncUIFromData();
            }));
            offsetContainer.Add(CreateFloatField("Y", data.particleOffset.y, value =>
            {
                data.particleOffset = new Vector3(data.particleOffset.x, value, data.particleOffset.z);
                particleCueNode.SyncUIFromData();
            }));
            offsetContainer.Add(CreateFloatField("Z", data.particleOffset.z, value =>
            {
                data.particleOffset = new Vector3(data.particleOffset.x, data.particleOffset.y, value);
                particleCueNode.SyncUIFromData();
            }));
            particleContent.Add(offsetContainer);

            var scaleContainer = new VisualElement { style = { marginBottom = 8 } };
            scaleContainer.Add(new Label("缩放") { style = { marginBottom = 4 } });
            scaleContainer.Add(CreateFloatField("X", data.particleScale.x, value =>
            {
                data.particleScale = new Vector3(value, data.particleScale.y, data.particleScale.z);
                particleCueNode.SyncUIFromData();
            }));
            scaleContainer.Add(CreateFloatField("Y", data.particleScale.y, value =>
            {
                data.particleScale = new Vector3(data.particleScale.x, value, data.particleScale.z);
                particleCueNode.SyncUIFromData();
            }));
            scaleContainer.Add(CreateFloatField("Z", data.particleScale.z, value =>
            {
                data.particleScale = new Vector3(data.particleScale.x, data.particleScale.y, value);
                particleCueNode.SyncUIFromData();
            }));
            particleContent.Add(scaleContainer);

            var attachToggle = new Toggle("附着目标") { value = data.attachToTarget };
            attachToggle.style.marginBottom = 4;
            attachToggle.RegisterValueChangedCallback(evt =>
            {
                data.attachToTarget = evt.newValue;
                particleCueNode.SyncUIFromData();
            });
            particleContent.Add(attachToggle);

            var loopingToggle = new Toggle("循环播放") { value = data.particleLoop };
            loopingToggle.style.marginBottom = 4;
            loopingToggle.RegisterValueChangedCallback(evt =>
            {
                data.particleLoop = evt.newValue;
                particleCueNode.SyncUIFromData();
                particleCueNode.NotifyConnectedTracksUpdateDuration();
            });
            particleContent.Add(loopingToggle);

            container.Add(particleSection);
        }
    }
}
