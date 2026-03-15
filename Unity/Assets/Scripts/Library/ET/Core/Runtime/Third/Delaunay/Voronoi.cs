/*
 * The author of this software is Steven Fortune.  Copyright (c) 1994 by AT&T
 * Bell Laboratories.
 * Permission to use, copy, modify, and distribute this software for any
 * purpose without fee is hereby granted, provided that this entire notice
 * is included in all copies of any software which is or includes a copy
 * or modification of this software and in all copies of the supporting
 * documentation for such software.
 * THIS SOFTWARE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED
 * WARRANTY.  IN PARTICULAR, NEITHER THE AUTHORS NOR AT&T MAKE ANY
 * REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY
 * OF THIS SOFTWARE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Mathematics;

namespace ET
{
    public sealed class Voronoi : IDisposable
    {
        private DelauSiteList _sites;
        private Dictionary<float2, DelauSite> _sitesIndexedByLocation;
        private List<DelauTriangle> _triangles;
        private List<DelauEdge> _edges;


        // TODO generalize this so it doesn't have to be a rectangle;
        // then we can make the fractal voronois-within-voronois
        private RectangleF _plotBounds;

        public RectangleF plotBounds
        {
            get { return _plotBounds; }
        }

        public void Dispose()
        {
            int i, n;
            if (_sites != null)
            {
                _sites.Dispose();
                _sites = null;
            }

            if (_triangles != null)
            {
                n = _triangles.Count;
                for (i = 0; i < n; ++i)
                {
                    _triangles[i].Dispose();
                }

                _triangles.Clear();
                _triangles = null;
            }

            if (_edges != null)
            {
                n = _edges.Count;
                for (i = 0; i < n; ++i)
                {
                    _edges[i].Dispose();
                }

                _edges.Clear();
                _edges = null;
            }

            //			_plotBounds = null;
            _sitesIndexedByLocation = null;
        }

        public Voronoi(List<float2> points, List<uint> colors, RectangleF plotBounds)
        {
            _sites = new DelauSiteList();
            _sitesIndexedByLocation = new Dictionary<float2, DelauSite>(); // XXX: Used to be Dictionary(true) -- weak refs. 
            AddSites(points, colors);
            _plotBounds = plotBounds;
            _triangles = new List<DelauTriangle>();
            _edges = new List<DelauEdge>();
            FortunesAlgorithm();
        }

        private void AddSites(List<float2> points, List<uint> colors)
        {
            int length = points.Count;
            for (int i = 0; i < length; ++i)
            {
                AddSite(points[i], (colors != null) ? colors[i] : 0, i);
            }
        }

        private void AddSite(float2 p, uint color, int index)
        {
            if (_sitesIndexedByLocation.ContainsKey(p))
                return; // Prevent duplicate site! (Adapted from https://github.com/nodename/as3delaunay/issues/1)
            float weight = RandomGenerator.RandFloat01() * 100f;
            DelauSite site = DelauSite.Create(p, (uint)index, weight, color);
            _sites.Add(site);
            _sitesIndexedByLocation[p] = site;
        }

        public List<DelauEdge> Edges()
        {
            return _edges;
        }

        public List<float2> Region(float2 p)
        {
            DelauSite site = _sitesIndexedByLocation[p];
            if (site == null)
            {
                return new List<float2>();
            }

            return site.Region(_plotBounds);
        }

        // TODO: bug: if you call this before you call region(), something goes wrong :(
        public List<float2> NeighborSitesForSite(float2 coord)
        {
            List<float2> points = new List<float2>();
            DelauSite site = _sitesIndexedByLocation[coord];
            if (site == null)
            {
                return points;
            }

            List<DelauSite> sites = site.NeighborSites();
            DelauSite neighbor;
            for (int nIndex = 0; nIndex < sites.Count; nIndex++)
            {
                neighbor = sites[nIndex];
                points.Add(neighbor.Coord);
            }

            return points;
        }

        public List<GeoCircle> Circles()
        {
            return _sites.Circles();
        }

        public List<GeoLineSegment> VoronoiBoundaryForSite(float2 coord)
        {
            return DelaunayHelpers.VisibleLineSegments(DelaunayHelpers.SelectEdgesForSitePoint(coord, _edges));
        }

        public List<GeoLineSegment> DelaunayLinesForSite(float2 coord)
        {
            return DelaunayHelpers.DelaunayLinesForEdges(DelaunayHelpers.SelectEdgesForSitePoint(coord, _edges));
        }

        public List<GeoLineSegment> VoronoiDiagram()
        {
            return DelaunayHelpers.VisibleLineSegments(_edges);
        }

        public List<GeoLineSegment> DelaunayTriangulation( /*BitmapData keepOutMask = null*/)
        {
            return DelaunayHelpers.DelaunayLinesForEdges(
                DelaunayHelpers.SelectNonIntersectingEdges( /*keepOutMask,*/_edges));
        }

        public List<GeoLineSegment> Hull()
        {
            return DelaunayHelpers.DelaunayLinesForEdges(HullEdges());
        }

        private List<DelauEdge> HullEdges()
        {
            return _edges.FindAll(delegate (DelauEdge edge) { return (edge.IsPartOfConvexHull()); });
        }

        public List<float2> HullPointsInOrder()
        {
            List<DelauEdge> hullEdges = HullEdges();

            List<float2> points = new List<float2>();
            if (hullEdges.Count == 0)
            {
                return points;
            }

            EdgeReorderer reorderer = new EdgeReorderer(hullEdges, VertexOrSite.SITE);
            hullEdges = reorderer.edges;
            List<DelauLRSide> orientations = reorderer.edgeOrientations;
            reorderer.Dispose();

            DelauLRSide orientation;

            int n = hullEdges.Count;
            for (int i = 0; i < n; ++i)
            {
                DelauEdge edge = hullEdges[i];
                orientation = orientations[i];
                points.Add(edge.Site(orientation).Coord);
            }

            return points;
        }

        public List<GeoLineSegment> SpanningTree(
            KruskalType type = KruskalType.MINIMUM /*, BitmapData keepOutMask = null*/)
        {
            List<DelauEdge> edges = DelaunayHelpers.SelectNonIntersectingEdges( /*keepOutMask,*/_edges);
            List<GeoLineSegment> segments = DelaunayHelpers.DelaunayLinesForEdges(edges);
            return DelaunayHelpers.Kruskal(segments, type);
        }

        public List<List<float2>> Regions()
        {
            return _sites.Regions(_plotBounds);
        }

        public List<uint> SiteColors( /*BitmapData referenceImage = null*/)
        {
            return _sites.SiteColors( /*referenceImage*/);
        }

        /**
         *
         * @param proximityMap a BitmapData whose regions are filled with the site index values; see PlanePointsCanvas::fillRegions()
         * @param x
         * @param y
         * @return coordinates of nearest Site to (x, y)
         *
         */
        public Nullable<float2> NearestSitePoint( /*BitmapData proximityMap,*/ float x, float y)
        {
            return _sites.NearestSitePoint( /*proximityMap,*/x, y);
        }

        public List<float2> SiteCoords()
        {
            return _sites.SiteCoords();
        }

        private DelauSite fortunesAlgorithm_bottomMostSite;

        private void FortunesAlgorithm()
        {
            DelauSite newSite, bottomSite, topSite, tempSite;
            DelauVertex v, vertex;
            float2 newintstar = float2.zero; //Because the compiler doesn't know that it will have a value - Julian
            DelauLRSide leftRight;
            Halfedge lbnd, rbnd, llbnd, rrbnd, bisector;
            DelauEdge edge;

            RectangleF dataBounds = _sites.GetSitesBounds();

            int sqrt_nsites = (int)(math.sqrt(_sites.Count + 4));
            HalfedgePriorityQueue heap = new HalfedgePriorityQueue(dataBounds.Y, dataBounds.Height, sqrt_nsites);
            DelauEdgeList edgeList = new DelauEdgeList(dataBounds.X, dataBounds.Width, sqrt_nsites);
            List<Halfedge> halfEdges = new List<Halfedge>();
            List<DelauVertex> vertices = new List<DelauVertex>();

            fortunesAlgorithm_bottomMostSite = _sites.Next();
            newSite = _sites.Next();

            for (; ; )
            {
                if (heap.Empty() == false)
                {
                    newintstar = heap.Min();
                }

                if (newSite != null
                    && (heap.Empty() || CompareByYThenX(newSite, newintstar) < 0))
                {
                    /* new site is smallest */
                    //trace("smallest: new site " + newSite);

                    // Step 8:
                    lbnd = edgeList.EdgeListLeftNeighbor(newSite.Coord); // the Halfedge just to the left of newSite
                    //trace("lbnd: " + lbnd);
                    rbnd = lbnd.edgeListRightNeighbor; // the Halfedge just to the right
                    //trace("rbnd: " + rbnd);
                    bottomSite = FortunesAlgorithm_rightRegion(lbnd); // this is the same as leftRegion(rbnd)
                    // this Site determines the region containing the new site
                    //trace("new Site is in region of existing site: " + bottomSite);

                    // Step 9:
                    edge = DelauEdge.CreateBisectingEdge(bottomSite, newSite);
                    //trace("new edge: " + edge);
                    _edges.Add(edge);

                    bisector = Halfedge.Create(edge, DelauLRSide.LEFT);
                    halfEdges.Add(bisector);
                    // inserting two Halfedges into edgeList constitutes Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // first half of Step 11:
                    if ((vertex = DelauVertex.Intersect(lbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(lbnd);
                        lbnd.vertex = vertex;
                        lbnd.ystar = vertex.y + newSite.Dist(vertex);
                        heap.Insert(lbnd);
                    }

                    lbnd = bisector;
                    bisector = Halfedge.Create(edge, DelauLRSide.RIGHT);
                    halfEdges.Add(bisector);
                    // second Halfedge for Step 10:
                    // insert bisector to the right of lbnd:
                    edgeList.Insert(lbnd, bisector);

                    // second half of Step 11:
                    if ((vertex = DelauVertex.Intersect(bisector, rbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.vertex = vertex;
                        bisector.ystar = vertex.y + newSite.Dist(vertex);
                        heap.Insert(bisector);
                    }

                    newSite = _sites.Next();
                }
                else if (heap.Empty() == false)
                {
                    /* intersection is smallest */
                    lbnd = heap.ExtractMin();
                    llbnd = lbnd.edgeListLeftNeighbor;
                    rbnd = lbnd.edgeListRightNeighbor;
                    rrbnd = rbnd.edgeListRightNeighbor;
                    bottomSite = FortunesAlgorithm_leftRegion(lbnd);
                    topSite = FortunesAlgorithm_rightRegion(rbnd);
                    // these three sites define a Delaunay triangle
                    // (not actually using these for anything...)
                    //_triangles.push(new Triangle(bottomSite, topSite, rightRegion(lbnd)));

                    v = lbnd.vertex;
                    v.SetIndex();
                    lbnd.edge.SetVertex((DelauLRSide)lbnd.leftRight, v);
                    rbnd.edge.SetVertex((DelauLRSide)rbnd.leftRight, v);
                    edgeList.Remove(lbnd);
                    heap.Remove(rbnd);
                    edgeList.Remove(rbnd);
                    leftRight = DelauLRSide.LEFT;
                    if (bottomSite.y > topSite.y)
                    {
                        tempSite = bottomSite;
                        bottomSite = topSite;
                        topSite = tempSite;
                        leftRight = DelauLRSide.RIGHT;
                    }

                    edge = DelauEdge.CreateBisectingEdge(bottomSite, topSite);
                    _edges.Add(edge);
                    bisector = Halfedge.Create(edge, leftRight);
                    halfEdges.Add(bisector);
                    edgeList.Insert(llbnd, bisector);
                    edge.SetVertex(DelauSideHelper.Other(leftRight), v);
                    if ((vertex = DelauVertex.Intersect(llbnd, bisector)) != null)
                    {
                        vertices.Add(vertex);
                        heap.Remove(llbnd);
                        llbnd.vertex = vertex;
                        llbnd.ystar = vertex.y + bottomSite.Dist(vertex);
                        heap.Insert(llbnd);
                    }

                    if ((vertex = DelauVertex.Intersect(bisector, rrbnd)) != null)
                    {
                        vertices.Add(vertex);
                        bisector.vertex = vertex;
                        bisector.ystar = vertex.y + bottomSite.Dist(vertex);
                        heap.Insert(bisector);
                    }
                }
                else
                {
                    break;
                }
            }

            // heap should be empty now
            heap.Dispose();
            edgeList.Dispose();

            for (int hIndex = 0; hIndex < halfEdges.Count; hIndex++)
            {
                Halfedge halfEdge = halfEdges[hIndex];
                halfEdge.ReallyDispose();
            }

            halfEdges.Clear();

            // we need the vertices to clip the edges
            for (int eIndex = 0; eIndex < _edges.Count; eIndex++)
            {
                edge = _edges[eIndex];
                edge.ClipVertices(_plotBounds);
            }

            // but we don't actually ever use them again!
            for (int vIndex = 0; vIndex < vertices.Count; vIndex++)
            {
                vertex = vertices[vIndex];
                vertex.Dispose();
            }

            vertices.Clear();
        }

        private DelauSite FortunesAlgorithm_leftRegion(Halfedge he)
        {
            DelauEdge edge = he.edge;
            if (edge == null)
            {
                return fortunesAlgorithm_bottomMostSite;
            }

            return edge.Site((DelauLRSide)he.leftRight);
        }

        private DelauSite FortunesAlgorithm_rightRegion(Halfedge he)
        {
            DelauEdge edge = he.edge;
            if (edge == null)
            {
                return fortunesAlgorithm_bottomMostSite;
            }

            return edge.Site(DelauSideHelper.Other((DelauLRSide)he.leftRight));
        }

        public static int CompareByYThenX(DelauSite s1, DelauSite s2)
        {
            if (s1.y < s2.y)
                return -1;
            if (s1.y > s2.y)
                return 1;
            if (s1.x < s2.x)
                return -1;
            if (s1.x > s2.x)
                return 1;
            return 0;
        }

        public static int CompareByYThenX(DelauSite s1, float2 s2)
        {
            if (s1.y < s2.y)
                return -1;
            if (s1.y > s2.y)
                return 1;
            if (s1.x < s2.x)
                return -1;
            if (s1.x > s2.x)
                return 1;
            return 0;
        }
    }
}