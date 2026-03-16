using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    public partial class SkillGraphView
    {
        public void LoadGraph(SkillGraphData graphData)
        {
            ClearGraph();

            var nodeMap = new Dictionary<string, SkillNodeBase>();
            foreach (NodeData nodeData in graphData.nodes)
            {
                SkillNodeBase node = NodeFactory.CreateNodeFromData(nodeData);
                if (node == null)
                {
                    continue;
                }

                AddElement(node);
                nodeMap[node.Guid] = node;
            }

            foreach (ConnectionData connection in graphData.connections)
            {
                if (string.IsNullOrEmpty(connection.outputNodeGuid) || string.IsNullOrEmpty(connection.inputNodeGuid))
                {
                    UnityEngine.Debug.LogWarning($"Skip invalid connection: outputNodeGuid={connection.outputNodeGuid}, inputNodeGuid={connection.inputNodeGuid}");
                    continue;
                }

                if (!nodeMap.TryGetValue(connection.outputNodeGuid, out SkillNodeBase outputNode))
                {
                    UnityEngine.Debug.LogWarning($"Output node not found: {connection.outputNodeGuid}");
                    continue;
                }

                if (!nodeMap.TryGetValue(connection.inputNodeGuid, out SkillNodeBase inputNode))
                {
                    UnityEngine.Debug.LogWarning($"Input node not found: {connection.inputNodeGuid}");
                    continue;
                }

                int outputPortId = connection.GetOutputPortId(outputNode.NodeType);
                int inputPortId = connection.GetInputPortId();
                if (outputPortId <= SkillPortId.Invalid || inputPortId <= SkillPortId.Invalid)
                {
                    UnityEngine.Debug.LogWarning(
                        $"Skip invalid connection ids: outputPortId={outputPortId}, inputPortId={inputPortId}");
                    continue;
                }

                Port outputPort = FindOutputPort(outputNode, outputPortId);
                if (outputPort == null)
                {
                    UnityEngine.Debug.LogWarning($"Output port not found: node={outputNode.Guid}, portId={outputPortId}");
                    continue;
                }

                Port inputPort = FindInputPort(inputNode, inputPortId);
                if (inputPort == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"Input port not found: node={inputNode.Guid}, inputPortId={inputPortId}");
                    continue;
                }

                Edge edge = outputPort.ConnectTo(inputPort);
                AddElement(edge);
            }
        }

        private Port FindOutputPort(SkillNodeBase node, int portId)
        {
            Port port = node.FindOutputPortByIdentifier(portId);
            if (port != null)
            {
                return port;
            }

            return node.Query<Port>()
                .ToList()
                .FirstOrDefault(candidate => candidate.direction == Direction.Output && SkillNodeBase.GetPortId(candidate) == portId);
        }

        private Port FindInputPort(SkillNodeBase node, int portId)
        {
            return node.inputContainer
                .Query<Port>()
                .ToList()
                .FirstOrDefault(candidate => SkillNodeBase.GetPortId(candidate) == portId);
        }

        public SkillGraphData SaveGraph(SkillGraphData graphData)
        {
            graphData.nodes.Clear();
            graphData.connections.Clear();

            foreach (GraphElement node in nodes)
            {
                if (node is not SkillNodeBase skillNode)
                {
                    continue;
                }

                NodeData nodeData = skillNode.SaveData();
                SkillNodeAssetPathUtility.SyncSerializedAssetPath(nodeData);
                graphData.nodes.Add(nodeData);
            }

            foreach (Edge edge in edges)
            {
                if (edge?.output == null || edge.input == null)
                {
                    continue;
                }

                if (edge.output.node is not SkillNodeBase outputNode || edge.input.node is not SkillNodeBase inputNode)
                {
                    continue;
                }

                int outputPortId = GetPortIdentifier(edge.output);
                int inputPortId = GetPortIdentifier(edge.input);
                if (outputPortId <= SkillPortId.Invalid || inputPortId <= SkillPortId.Invalid)
                {
                    UnityEngine.Debug.LogWarning(
                        $"Skip invalid edge: {outputNode.Guid}->{inputNode.Guid}, outputPortId={outputPortId}, inputPortId={inputPortId}");
                    continue;
                }

                graphData.connections.Add(new ConnectionData
                {
                    outputNodeGuid = outputNode.Guid,
                    outputPortId = outputPortId,
                    inputNodeGuid = inputNode.Guid,
                    inputPortId = inputPortId
                });
            }

            return graphData;
        }

        private int GetPortIdentifier(Port port)
        {
            int portId = SkillNodeBase.GetPortId(port);
            if (portId > SkillPortId.Invalid)
            {
                return portId;
            }

            UnityEngine.Debug.LogWarning($"Invalid port identifier: portName={port.portName}, name={port.name}");
            return SkillPortId.Invalid;
        }
    }
}
