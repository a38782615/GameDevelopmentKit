using System;
using Unity.Mathematics;
namespace ET.Client
{
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
