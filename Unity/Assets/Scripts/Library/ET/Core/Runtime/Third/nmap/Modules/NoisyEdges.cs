// Annotate each edge with a noisy path, to make maps look more interesting.
// Author: amitp@cs.stanford.edu
// License: MIT

using System.Collections.Generic;
using Unity.Mathematics;

namespace ET
{
    public class NoisyEdges
    {
        [StaticField]
        private static readonly float NOISY_LINE_TRADEOFF = 0.5f; // low: jagged vedge; high: jagged dedge

        public Dictionary<int, List<float2>>
            path0 = new Dictionary<int, List<float2>>(); // edge index -> Vector.<Point>

        public Dictionary<int, List<float2>>
            path1 = new Dictionary<int, List<float2>>(); // edge index -> Vector.<Point>

        private const float SizeScale = 0.1f;
        Random random;
        public NoisyEdges(ref Random r)
        {
            random = r;
        }

        // Build noisy line paths for each of the Voronoi edges. There are
        // two noisy line paths for each edge, each covering half the
        // distance: path0 is from v0 to the midpoint and path1 is from v1
        // to the midpoint. When drawing the polygons, one or the other
        // must be drawn in reverse order.
        public void BuildNoisyEdges(BiomeMap biomeMap)
        {
            foreach (MapCenter p in biomeMap.MapGraph.centers)
            {
                foreach (MapEdge edge in p.borders)
                {
                    if (edge.d0 != null && edge.d1 != null && edge.v0 != null && edge.v1 != null
                        && !path0.ContainsKey(edge.index))
                    {
                        float f = NOISY_LINE_TRADEOFF;
                        float2 t = MathExtensions.Interpolate(edge.v0.point, edge.d0.point, f);
                        float2 q = MathExtensions.Interpolate(edge.v0.point, edge.d1.point, f);
                        float2 r = MathExtensions.Interpolate(edge.v1.point, edge.d0.point, f);
                        float2 s = MathExtensions.Interpolate(edge.v1.point, edge.d1.point, f);

                        float minLength = 10 * SizeScale;
                        if (edge.d0.biome != edge.d1.biome)
                        {
                            minLength = 3 * SizeScale;
                        }

                        if (edge.d0.ocean && edge.d1.ocean)
                        {
                            minLength = 100 * SizeScale;
                        }

                        if (edge.d0.coast || edge.d1.coast)
                        {
                            minLength = 1 * SizeScale;
                        }

                        if (edge.river > 0)
                        {
                            minLength = 1 * SizeScale;
                        }

                        path0[edge.index] = buildNoisyLineSegments(edge.v0.point, t, edge.midpoint, q, minLength);
                        path1[edge.index] = buildNoisyLineSegments(edge.v1.point, s, edge.midpoint, r, minLength);
                    }
                }
            }
        }

        // Helper function: build a single noisy line in a quadrilateral A-B-C-D,
        // and store the output points in a Vector.
        private List<float2> buildNoisyLineSegments(float2 A, float2 B, float2 C, float2 D, float minLength)
        {
            List<float2> points = new List<float2>();

            points.Add(A);
            subdivide(A, B, C, D, points, minLength);
            points.Add(C);

            return points;
        }

        private void subdivide(float2 A, float2 B, float2 C, float2 D, List<float2> points, float minLength)
        {
            if (math.distance(A, C) < minLength || math.distance(B, D) < minLength)
                return;

            // Subdivide the quadrilateral
            float p = random.NextFloat(0.2f, 0.8f); // vertical (along A-D and B-C)
            float q = random.NextFloat(0.2f, 0.8f); // horizontal (along A-B and D-C)

            // Midpoints
            float2 E = MathExtensions.Interpolate(A, D, p);
            float2 F = MathExtensions.Interpolate(B, C, p);
            float2 G = MathExtensions.Interpolate(A, B, q);
            float2 I = MathExtensions.Interpolate(D, C, q);

            // Central point
            float2 H = MathExtensions.Interpolate(E, F, q);

            // Divide the quad into subquads, but meet at H
            float s = 1 - random.NextFloat(-0.4f, 0.4f);
            float t = 1 - random.NextFloat(-0.4f, 0.4f);

            subdivide(A, MathExtensions.Interpolate(G, B, s), H, MathExtensions.Interpolate(E, D, t), points,
                minLength);
            points.Add(H);
            subdivide(H, MathExtensions.Interpolate(F, C, s), C, MathExtensions.Interpolate(I, D, t), points,
                minLength);
        }
    }
}