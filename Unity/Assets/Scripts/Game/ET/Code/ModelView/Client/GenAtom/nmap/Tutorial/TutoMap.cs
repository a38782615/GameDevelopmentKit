using RectangleF = ET.Geometry.RectangleF;
using Random = Unity.Mathematics.Random;
using Unity.Mathematics;
using System.Collections.Generic;
using System;

namespace ET
{
    [EnableClass]
    public class TutoMap
    {
        private int _pointCount = 500;
        float _lakeThreshold = 0.3f;
        public int Width = 50;
        public int Height = 50;
        const int NUM_LLOYD_RELAXATIONS = 2;

        public MapGraph MapGraph { get; private set; }
        public MapCenter SelectedMapCenter { get; private set; }
        private Random random;
        public TutoMap()
        {
        }
        public void Init(uint seed, Func<float2, bool> checkIsland = null)
        {
            random = Random.CreateFromIndex(seed);
            List<uint> colors = new List<uint>();
            var points = new List<float2>();

            for (int i = 0; i < _pointCount; i++)
            {
                colors.Add(0);
                points.Add(new float2(
                    random.NextFloat(0, Width),
                    random.NextFloat(0, Height))
                );
            }

            for (int i = 0; i < NUM_LLOYD_RELAXATIONS; i++)
            {
                var fp = MapGraph.RelaxPoints(points, Width, Height);
                points.Clear();
                points.AddRange(fp);
            }

            var voronoi = new Voronoi(points, colors, new RectangleF(0, 0, Width, Height));

            checkIsland = checkIsland ?? IslandShape.makePerlin();
            MapGraph = new MapGraph(checkIsland, points, voronoi, (int)Width, (int)Height, _lakeThreshold);
        }
    }
}
