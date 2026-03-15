using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Serialization;

namespace ET.Client
{
    [Serializable]
    public class AttributeModifierData : Object
    {
        public AttrType attrType = AttrType.None;
        public ModifierOperation operation = ModifierOperation.Add;
        public ModifierMagnitudeSourceType magnitudeSourceType = ModifierMagnitudeSourceType.FixedValue;
        public float fixedValue = 0f;
        public string formula = "";
        public MMCType mmcType = MMCType.AttributeBased;
        public string setByCallerKey = "";
        public AttrType mmcCaptureAttribute = AttrType.Attack;
        public MMCAttributeSource mmcAttributeSource = MMCAttributeSource.Source;
        public float mmcCoefficient = 1f;
        public bool mmcUseSnapshot = true;
    }

    public enum MMCAttributeSource
    {
        Source,
        Target
    }

    [Serializable]
    public abstract class NodeData : Object
    {
        public string guid;
        public NodeType nodeType;
        public float2 position;
        public TargetType targetType = TargetType.Caster;
    }

    [Serializable]
    public class ConnectionData : Object
    {
        public string outputNodeGuid;
        public int outputPortId;
        [FormerlySerializedAs("outputPortName")]
        public string legacyOutputPortName;
        public string inputNodeGuid;
        public int inputPortId;
        public string inputPortName;

        public int GetOutputPortId(NodeType nodeType)
        {
            if (this.outputPortId > SkillPortId.Invalid)
            {
                return this.outputPortId;
            }

            this.outputPortId = SkillPortIdUtility.ResolveLegacyOutputPortId(nodeType, this.legacyOutputPortName);
            return this.outputPortId;
        }

        public int GetInputPortId()
        {
            if (this.inputPortId > SkillPortId.Invalid)
            {
                return this.inputPortId;
            }

            this.inputPortId = SkillPortIdUtility.ResolveLegacyInputPortId(this.inputPortName);
            return this.inputPortId;
        }
    }
}
