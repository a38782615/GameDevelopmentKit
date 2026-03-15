using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GraphViewEdge = UnityEditor.Experimental.GraphView.Edge;

namespace ET.Client.Editor
{
    /// <summary>
    /// 自定义时间轴端口，统一派发连接变化事件。
    /// </summary>
    public class TimelinePort : Port
    {
        public event Action OnConnectionChanged;

        protected TimelinePort(Orientation portOrientation, Direction portDirection, Capacity portCapacity, Type type)
            : base(portOrientation, portDirection, portCapacity, type)
        {
        }

        public static TimelinePort Create(Orientation orientation, Direction direction, Capacity capacity, Type type)
        {
            var connectorListener = new DefaultEdgeConnectorListener();
            var port = new TimelinePort(orientation, direction, capacity, type)
            {
                m_EdgeConnector = new EdgeConnector<GraphViewEdge>(connectorListener)
            };

            port.AddManipulator(port.m_EdgeConnector);
            return port;
        }

        public override void DisconnectAll()
        {
            base.DisconnectAll();
            NotifyConnectionChanged();
        }

        private void NotifyConnectionChanged()
        {
            schedule.Execute(() =>
            {
                OnConnectionChanged?.Invoke();
            }).ExecuteLater(50);
        }

        private static void NotifyPortChanged(Port port, ISet<TimelinePort> changedPorts)
        {
            if (port is TimelinePort timelinePort && changedPorts.Add(timelinePort))
            {
                timelinePort.NotifyConnectionChanged();
            }
        }

        /// <summary>
        /// 处理 GraphView 默认连线行为，并补发端口变化通知。
        /// </summary>
        private class DefaultEdgeConnectorListener : IEdgeConnectorListener
        {
            public void OnDropOutsidePort(GraphViewEdge edge, Vector2 position)
            {
            }

            public void OnDrop(GraphView graphView, GraphViewEdge edge)
            {
                var changedPorts = new HashSet<TimelinePort>();
                var edgesToCreate = new List<GraphViewEdge> { edge };
                var edgesToDelete = new List<GraphElement>();

                NotifyPortChanged(edge.input, changedPorts);
                NotifyPortChanged(edge.output, changedPorts);

                if (edge.input.capacity == Capacity.Single)
                {
                    foreach (GraphViewEdge connection in edge.input.connections)
                    {
                        if (connection == edge)
                        {
                            continue;
                        }

                        edgesToDelete.Add(connection);
                        NotifyPortChanged(connection.input, changedPorts);
                        NotifyPortChanged(connection.output, changedPorts);
                    }
                }

                if (edge.output.capacity == Capacity.Single)
                {
                    foreach (GraphViewEdge connection in edge.output.connections)
                    {
                        if (connection == edge)
                        {
                            continue;
                        }

                        edgesToDelete.Add(connection);
                        NotifyPortChanged(connection.input, changedPorts);
                        NotifyPortChanged(connection.output, changedPorts);
                    }
                }

                if (edgesToDelete.Count > 0)
                {
                    graphView.DeleteElements(edgesToDelete);
                }

                if (graphView.graphViewChanged != null)
                {
                    edgesToCreate = graphView.graphViewChanged(new GraphViewChange
                    {
                        edgesToCreate = edgesToCreate
                    }).edgesToCreate;
                }

                if (edgesToCreate == null)
                {
                    return;
                }

                foreach (GraphViewEdge currentEdge in edgesToCreate)
                {
                    graphView.AddElement(currentEdge);
                    currentEdge.input.Connect(currentEdge);
                    currentEdge.output.Connect(currentEdge);

                    NotifyPortChanged(currentEdge.input, changedPorts);
                    NotifyPortChanged(currentEdge.output, changedPorts);
                }
            }
        }
    }
}
