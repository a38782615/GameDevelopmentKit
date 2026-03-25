using ET;
using System;
using System.Collections.Generic;
using RectangleF = ET.Geometry.RectangleF;
using System.Linq;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace ET
{
    // BiomeMap 是地图生成流程的入口对象。
    // 它本身不负责地形推导，而是负责准备随机采样点、做 Lloyd Relaxation，
    // 然后把整理后的输入交给 MapGraph，生成带有海洋/河流/湿度/群系信息的图结构。
    public class BiomeMap
    {
        // Voronoi 采样点数量，数量越多，最终地图多边形越密。
        private int _pointCount = 500;
        // 一个多边形中“水角点”占比达到这个阈值时，会被判定为湖泊/水域。
        float _lakeThreshold = 0.1f;
        public int Width;
        public int Height;
        // Lloyd Relaxation 会把随机点重新分布得更均匀，避免 Voronoi 多边形过于尖锐或密度失衡。
        const int NUM_LLOYD_RELAXATIONS = 2;

        // MapGraph 才是核心结果，里面保存中心点、角点、边、河流、湿度和 biome。
        public MapGraph MapGraph { get; private set; }
        public MapCenter SelectedMapCenter { get; private set; }
        List<uint> colors = new List<uint>();

        public BiomeMap(float2 wh)
        {
            Width = (int)wh.x;
            Height = (int)wh.y;
        }

        public void SetPointNum(int num)
        {
            _pointCount = num;
        }

        public void SetLakeThreshold(float lake)
        {
            this._lakeThreshold = lake;
        }

        private Random random;

        // 原始 Voronoi 站点。初始化时先随机撒点，再做松弛。
        List<float2> points = new List<float2>();

        public void Init(uint seed, Func<float2, bool> checkIsland = null)
        {
            random = Random.CreateFromIndex(seed);
            points.Clear();
            colors.Clear();

            for (int i = 0; i < _pointCount; i++)
            {
                colors.Add(0);
                // 在矩形地图范围内均匀随机撒点，作为 Voronoi 的站点。
                points.Add(new float2(
                    random.NextFloat(0, Width),
                    random.NextFloat(0, Height))
                );
            }

            for (int i = 0; i < NUM_LLOYD_RELAXATIONS; i++)
            {
                // 把站点移动到各自 Voronoi 区域的质心附近，让区域分布更规则。
                points = MapGraph.RelaxPoints(points, Width, Height).ToList();
            }

            // Voronoi 负责给出初始的多边形划分，后续再由 MapGraph 推导高程、水系和群系。
            var voronoi = new Voronoi(points, colors, new RectangleF(0, 0, Width, Height));

            // MapGraph 会基于 Voronoi 结果构建：
            // 1. 拓扑关系（center/corner/edge）
            // 2. 高程、海洋、海岸、湖泊
            // 3. 下坡流向、流域、河流、湿度
            // 4. 最终 biome 分类
            MapGraph = new MapGraph(checkIsland, points, voronoi, (int)Width, (int)Height, _lakeThreshold, seed);
        }
    }
}
