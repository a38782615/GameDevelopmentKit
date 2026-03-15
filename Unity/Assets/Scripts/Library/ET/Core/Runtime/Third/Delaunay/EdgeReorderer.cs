using System;
using System.Collections.Generic;

namespace ET
{
    public enum VertexOrSite
    {
        VERTEX,
        SITE
    }

    sealed class EdgeReorderer : IDisposable
    {
        private List<DelauEdge> _edges;
        private List<DelauLRSide> _edgeOrientations;

        public List<DelauEdge> edges
        {
            get { return _edges; }
        }

        public List<DelauLRSide> edgeOrientations
        {
            get { return _edgeOrientations; }
        }

        public EdgeReorderer(List<DelauEdge> origEdges, VertexOrSite criterion)
        {
            _edges = new List<DelauEdge>();
            _edgeOrientations = new List<DelauLRSide>();
            if (origEdges.Count > 0)
            {
                _edges = ReorderEdges(origEdges, criterion);
            }
        }

        public void Dispose()
        {
            _edges = null;
            _edgeOrientations = null;
        }

        private List<DelauEdge> ReorderEdges(List<DelauEdge> origEdges, VertexOrSite criterion)
        {
            int i;
            int n = origEdges.Count;
            DelauEdge edge;
            // we're going to reorder the edges in order of traversal
            bool[] done = new bool[n];
            int nDone = 0;
            for (int j = 0; j < n; j++)
            {
                done[j] = false;
            }

            List<DelauEdge> newEdges = new List<DelauEdge>(); // TODO: Switch to Deque if performance is a concern

            i = 0;
            edge = origEdges[i];
            newEdges.Add(edge);
            _edgeOrientations.Add(DelauLRSide.LEFT);
            IDelauCoord firstPoint = (criterion == VertexOrSite.VERTEX) ? (IDelauCoord)edge.leftVertex : (IDelauCoord)edge.leftSite;
            IDelauCoord lastPoint = (criterion == VertexOrSite.VERTEX) ? (IDelauCoord)edge.rightVertex : (IDelauCoord)edge.rightSite;

            if (firstPoint == DelauVertex.VERTEX_AT_INFINITY || lastPoint == DelauVertex.VERTEX_AT_INFINITY)
            {
                return new List<DelauEdge>();
            }

            done[i] = true;
            ++nDone;

            while (nDone < n)
            {
                for (i = 1; i < n; ++i)
                {
                    if (done[i])
                    {
                        continue;
                    }

                    edge = origEdges[i];
                    IDelauCoord leftPoint = (criterion == VertexOrSite.VERTEX)
                        ? (IDelauCoord)edge.leftVertex
                        : (IDelauCoord)edge.leftSite;
                    IDelauCoord rightPoint = (criterion == VertexOrSite.VERTEX)
                        ? (IDelauCoord)edge.rightVertex
                        : (IDelauCoord)edge.rightSite;
                    if (leftPoint == DelauVertex.VERTEX_AT_INFINITY || rightPoint == DelauVertex.VERTEX_AT_INFINITY)
                    {
                        return new List<DelauEdge>();
                    }

                    if (leftPoint == lastPoint)
                    {
                        lastPoint = rightPoint;
                        _edgeOrientations.Add(DelauLRSide.LEFT);
                        newEdges.Add(edge);
                        done[i] = true;
                    }
                    else if (rightPoint == firstPoint)
                    {
                        firstPoint = leftPoint;
                        _edgeOrientations.Insert(0, DelauLRSide.LEFT); // TODO: Change datastructure if this is slow
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }
                    else if (leftPoint == firstPoint)
                    {
                        firstPoint = rightPoint;
                        _edgeOrientations.Insert(0, DelauLRSide.RIGHT);
                        newEdges.Insert(0, edge);
                        done[i] = true;
                    }
                    else if (rightPoint == lastPoint)
                    {
                        lastPoint = leftPoint;
                        _edgeOrientations.Add(DelauLRSide.RIGHT);
                        newEdges.Add(edge);
                        done[i] = true;
                    }

                    if (done[i])
                    {
                        ++nDone;
                    }
                }
            }

            return newEdges;
        }
    }
}