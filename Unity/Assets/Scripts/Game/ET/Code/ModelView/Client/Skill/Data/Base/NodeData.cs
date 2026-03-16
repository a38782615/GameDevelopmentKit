using System;
using System.Collections.Generic;
using Unity.Mathematics;
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
        public string inputNodeGuid;
        public int inputPortId;

        public int GetOutputPortId(NodeType nodeType)
        {
            return this.outputPortId;
        }

        public int GetInputPortId()
        {
            return this.inputPortId;
        }
    }
}
