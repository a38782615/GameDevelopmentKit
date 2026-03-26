using ET;
using System;
using System.Collections.Generic;
using RectangleF = ET.Geometry.RectangleF;
using System.Linq;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace ET
{
    // MapGraph 是地图生成的核心结果对象。
    // 它把 Voronoi 输出转换成可计算的拓扑图，并在这张图上继续推导：
    // 1. 海洋/海岸/湖泊
    // 2. 高程与下坡方向
    // 3. 流域与河流
    // 4. 湿度与最终群系
    public class MapGraph
    {
        // 用近似 x 坐标做桶分组，避免 Voronoi 重复角点被反复创建。
        List<KeyValuePair<int, MapCorner>> _cornerMap = new List<KeyValuePair<int, MapCorner>>();
        // 岛屿判定函数：true 表示陆地，false 表示水域。
        Func<float2, bool> inside;
        bool _needsMoreRandomness;
        private Random random;
        private float2 _elevationNoiseOffset;
        private float2 _temperatureNoiseOffset;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public List<MapCenter> centers = new List<MapCenter>();
        public List<MapCorner> corners = new List<MapCorner>();
        public List<MapEdge> edges = new List<MapEdge>();

        // 只返回内陆角点。海洋和海岸角点在后续重分布时不参与排序。
        private List<MapCorner> LandCorners
        {
            get
            {
                List<MapCorner> result = new List<MapCorner>(corners.Count);
                foreach (MapCorner corner in corners)
                {
                    if (!corner.ocean && !corner.coast)
                    {
                        result.Add(corner);
                    }
                }

                return result;
            }
        }
        public MapGraph(Func<float2, bool> checkIsland, IEnumerable<float2> points, Voronoi voronoi, int width, int height, uint seed)
        {
            Init(checkIsland, points, voronoi, width, height, seed);
        }

        void Init(Func<float2, bool> checkIsland, IEnumerable<float2> points, Voronoi voronoi, int width, int height, uint seed)
        {
            Width = width;
            Height = height;
            inside = checkIsland;
            random = Random.CreateFromIndex(seed);
            _elevationNoiseOffset = new float2(random.NextFloat(13f, 97f), random.NextFloat(29f, 131f));
            _temperatureNoiseOffset = new float2(random.NextFloat(41f, 173f), random.NextFloat(59f, 211f));
            // 生成顺序是先搭建图，再逐步往图上附加地理属性。
            // 这里不是“先有高度图再裁海岸线”，而是先从岛屿形状和 Voronoi 拓扑出发，
            // 反推适合当前海岸线的高度、水流和湿度分布。
            BuildGraph(points, voronoi);

            AssignCornerWater();
            AssignOceanCoastAndLand();

            // 高程只保留为低起伏排水坡度，不再作为地形分类主驱动。
            AssignCornerElevations();
            RedistributeElevations();
            AssignPolygonElevations();

            // 高程稳定后，才能得到水流方向和流域。
            CalculateDownslopes();
            CalculateWatersheds();

            // 河流依赖下坡链路，因此在流域之后生成。
            CreateRivers();

            // 湿度先在角点传播，再聚合到多边形中心。
            AssignCornerMoisture();
            RedistributeMoisture();
            AssignPolygonMoisture();

            // 最终用温度和湿度共同决定群系类型。
            AssignCornerTemperature();
            AssignPolygonTemperature();
            foreach (MapCenter center in centers)
            {
                center.biome = GetBiome(center);
            }
        }

        private void BuildGraph(IEnumerable<float2> points, ET.Voronoi voronoi)
        {
            // 把 Voronoi 的几何结果转换成项目自己的图结构：
            // center 表示多边形中心，corner 表示多边形顶点，edge 同时连接二者。
            var libedges = voronoi.Edges();

            var centerLookup = new Dictionary<float2?, MapCenter>();

            // 每个采样点先对应一个 MapCenter，后面通过坐标回查。
            foreach (var point in points)
            {
                var p = new MapCenter { index = centers.Count, point = point };
                centers.Add(p);
                centerLookup[point] = p;
            }

            // 这里先触发一次 Region，是对 Voronoi 库行为的兼容处理。
            foreach (var p in centers)
            {
                voronoi.Region(p.point);
            }

            foreach (var libedge in libedges)
            {
                var dedge = libedge.DelaunayLine();
                var vedge = libedge.VoronoiEdge();

                // 一条逻辑边同时记录：
                // v0/v1: Voronoi 顶点
                // d0/d1: 这条边两侧的多边形中心
                var edge = new MapEdge
                {
                    index = edges.Count,
                    river = 0,

                    v0 = MakeCorner(vedge.p0),
                    v1 = MakeCorner(vedge.p1),
                    d0 = centerLookup[dedge.p0],
                    d1 = centerLookup[dedge.p1]
                };
                if (vedge.p0.HasValue && vedge.p1.HasValue)
                {
                    edge.midpoint = MathExtensions.Interpolate(vedge.p0.Value, vedge.p1.Value, 0.5f);
                }

                edges.Add(edge);

                // 建立 center <-> edge、corner <-> edge 的直接引用。
                if (edge.d0 != null)
                {
                    edge.d0.borders.Add(edge);
                }

                if (edge.d1 != null)
                {
                    edge.d1.borders.Add(edge);
                }

                if (edge.v0 != null)
                {
                    edge.v0.protrudes.Add(edge);
                }

                if (edge.v1 != null)
                {
                    edge.v1.protrudes.Add(edge);
                }

                // 由共享边得到相邻多边形。
                if (edge.d0 != null && edge.d1 != null)
                {
                    AddToCenterList(edge.d0.neighbors, edge.d1);
                    AddToCenterList(edge.d1.neighbors, edge.d0);
                }

                // 由同一条 Voronoi 边得到相邻角点。
                if (edge.v0 != null && edge.v1 != null)
                {
                    AddToCornerList(edge.v0.adjacent, edge.v1);
                    AddToCornerList(edge.v1.adjacent, edge.v0);
                }

                // 多边形中心记录它拥有的角点。
                if (edge.d0 != null)
                {
                    AddToCornerList(edge.d0.corners, edge.v0);
                    AddToCornerList(edge.d0.corners, edge.v1);
                }

                if (edge.d1 != null)
                {
                    AddToCornerList(edge.d1.corners, edge.v0);
                    AddToCornerList(edge.d1.corners, edge.v1);
                }

                // 角点反向记录它接触到的多边形。
                if (edge.v0 != null)
                {
                    AddToCenterList(edge.v0.touches, edge.d0);
                    AddToCenterList(edge.v0.touches, edge.d1);
                }

                if (edge.v1 != null)
                {
                    AddToCenterList(edge.v1.touches, edge.d0);
                    AddToCenterList(edge.v1.touches, edge.d1);
                }
            }

            // 有些边界角点不会完整出现在 Voronoi 返回值里，这里手补四个外框角，
            // 否则后面的多边形填充会缺口。
            var topLeft = centers.OrderBy(p => p.point.x + p.point.y).First();
            AddCorner(topLeft, 0, 0);

            var bottomRight = centers.OrderByDescending(p => p.point.x + p.point.y).First();
            AddCorner(bottomRight, Width, Height);

            var topRight = centers.OrderByDescending(p => Width - p.point.x + p.point.y).First();
            AddCorner(topRight, 0, Height);

            var bottomLeft = centers.OrderByDescending(p => p.point.x + Height - p.point.y).First();
            AddCorner(bottomLeft, Width, 0);

            // 多边形角点按顺时针排序，便于后续渲染和填充。
            foreach (var center in centers)
            {
                center.corners.Sort(ClockwiseComparison(center));
            }
        }

        private static void AddCorner(MapCenter topLeft, int x, int y)
        {
            // 如果这个中心点本身不在外框角上，就补一个边界角点进去。
            if (topLeft.point.x != x || topLeft.point.y != y)
                topLeft.corners.Add(new MapCorner { ocean = true, point = new float2(x, y) });
        }

        private Comparison<MapCorner> ClockwiseComparison(MapCenter mapCenter)
        {
            // 用叉积符号比较相对中心点的旋转方向，达到极角排序的效果。
            Comparison<MapCorner> result =
                (a, b) =>
                {
                    return (int)(((a.point.x - mapCenter.point.x) * (b.point.y - mapCenter.point.y) - (b.point.x - mapCenter.point.x) * (a.point.y - mapCenter.point.y)) * 1000);
                };
            return result;
        }

        private MapCorner MakeCorner(float2? nullablePoint)
        {
            if (nullablePoint == null)
                return null;

            var point = nullablePoint.Value;

            // Voronoi 库可能为同一个几何顶点返回多个实例。
            // 这里按近似 x 坐标分桶，再做距离判定，把它们规范成同一个 MapCorner。
            for (var i = (int)(point.x - 1); i <= (int)(point.x + 1); i++)
            {
                for (int j = 0; j < _cornerMap.Count; j++)
                {
                    KeyValuePair<int, MapCorner> kvp = _cornerMap[j];
                    if (kvp.Key != i)
                    {
                        continue;
                    }

                    var dx = point.x - kvp.Value.point.x;
                    var dy = point.y - kvp.Value.point.y;
                    if (dx * dx + dy * dy < 1e-6)
                        return kvp.Value;
                }
            }

            var corner = new MapCorner { index = corners.Count, point = point };
            corners.Add(corner);
            corner.border = point.x == 0 || point.x == Width || point.y == 0 || point.y == Height;

            _cornerMap.Add(new KeyValuePair<int, MapCorner>((int)(point.x), corner));

            return corner;
        }

        private void AddToCornerList(List<MapCorner> v, MapCorner x)
        {
            // 图结构里大量依赖唯一邻接关系，这里统一防空、防重复。
            if (x != null && v.IndexOf(x) < 0)
                v.Add(x);
        }

        private void AddToCenterList(List<MapCenter> v, MapCenter x)
        {
            if (x != null && v.IndexOf(x) < 0)
            {
                v.Add(x);
            }
        }

        private void AssignCornerWater()
        {
            foreach (MapCorner q in corners)
            {
                // 初始阶段只根据岛屿轮廓判断角点是否落在水域中。
                // 后续会在 AssignOceanCoastAndLand 中继续细分 ocean / coast / inland water。
                bool isWater = !inside(q.point);
                q.water = isWater;
                q.ocean = isWater;
            }
        }

        private void AssignCornerElevations()
        {
            // 高程主趋势改为“左高右低”，让水系更容易朝右侧入海口排出。
            // 低振幅噪声只负责打散边界，避免坡线过于机械。
            float widthScale = math.max(1f, Width);
            float heightScale = math.max(1f, Height);
            foreach (MapCorner q in corners)
            {
                float rightLowGradient01 = math.saturate(1f - q.point.x / widthScale);
                float centerBias01 = 1f - math.saturate(math.abs(q.point.y / heightScale * 2f - 1f));
                float estuaryMask =
                    math.saturate((q.point.x / widthScale - 0.66f) / 0.22f) *
                    math.saturate((centerBias01 - 0.35f) / 0.45f);
                float noise = SampleSignedNoise(q.point, _elevationNoiseOffset, 0.045f, 2);
                float elevation = 0.04f + rightLowGradient01 * 0.24f + centerBias01 * 0.03f + noise * 0.02f;
                elevation -= estuaryMask * 0.08f;
                q.elevation = math.saturate(elevation);
            }
        }

        private void AssignOceanCoastAndLand()
        {
            // 先把角点级别的水信息汇总到多边形中心，再从中心反写回角点。
            // 这里区分三个概念：
            // water: 任何水域
            // ocean: 仅位于地图边缘带的水域
            // coast: 同时接触海洋和陆地的过渡带
            foreach (MapCenter p in centers)
            {
                int numWater = 0;
                bool centerIsLand = inside(p.point);
                p.border = false;
                foreach (MapCorner q in p.corners)
                {
                    if (q.border)
                    {
                        p.border = true;
                    }

                    if (q.water)
                    {
                        numWater++;
                    }
                }

                // 海洋只存在在边缘带；内陆湖泊、河道即便连通也不会被并入海洋。
                p.water = !centerIsLand || numWater >= p.corners.Count * 0.015f;
                p.ocean = p.water && IsOceanBand(p.point);
            }

            // 同时挨着海和陆地的多边形就是海岸。
            foreach (MapCenter p in centers)
            {
                int numOcean = 0;
                int numLand = 0;
                foreach (MapCenter r in p.neighbors)
                {
                    numOcean += r.ocean ? 1 : 0;
                    numLand += !r.water ? 1 : 0;
                }

                p.coast = !p.water && (numOcean > 0);
            }

            // 最后把中心级别的判定再同步回角点，保证 corner/center 语义一致。
            foreach (MapCorner q in corners)
            {
                int numOcean = 0;
                int numLand = 0;
                foreach (MapCenter p in q.touches)
                {
                    numOcean += p.ocean ? 1 : 0;
                    numLand += !p.water ? 1 : 0;
                }

                q.coast = (numOcean > 0) && (numLand > 0);
                q.water = q.border || ((numLand != q.touches.Count) && !q.coast);
                q.ocean = q.water && (q.border || ((numOcean > 0) && IsOceanBand(q.point)));
            }
        }

        private void RedistributeElevations()
        {
            // 压低整体起伏，只保留平缓地势，避免生成“高山”观感。
            List<MapCorner> locations = LandCorners;
            if (locations.Count == 0)
            {
                return;
            }

            locations.Sort((a, b) => a.elevation.CompareTo(b.elevation));
            if (locations.Count == 1)
            {
                locations[0].elevation = 0.18f;
            }
            else
            {
                for (int i = 0; i < locations.Count; i++)
                {
                    float y = (float)i / (locations.Count - 1);
                    locations[i].elevation = math.lerp(0.06f, 0.32f, y);
                }
            }
        }

        private void AssignPolygonElevations()
        {
            // 多边形高程取其所有角点高程的平均值。
            foreach (var p in centers)
            {
                var sumElevation = 0.0f;
                foreach (var q in p.corners)
                {
                    sumElevation += q.elevation;
                }

                p.elevation = sumElevation / p.corners.Count;
            }
        }

        private void CalculateDownslopes()
        {
            // 每个角点记录一个“最低相邻点”作为下坡方向。
            // 这相当于为后续河流和流域计算搭一张有向图。
            foreach (var q in corners)
            {
                var r = q;
                foreach (var s in q.adjacent)
                {
                    if (s.elevation <= r.elevation)
                    {
                        r = s;
                    }
                }

                q.downslope = r;
            }
        }

        private void CalculateWatersheds()
        {
            // watershed 表示这个角点最终汇入哪一条出海路径。
            // 初始时先指向一步下坡点，后面再沿着下坡链不断压缩。
            foreach (var q in corners)
            {
                q.watershed = q;
                if (!q.ocean && !q.coast)
                {
                    q.watershed = q.downslope;
                }
            }

            // 反复沿下坡关系追踪，直到稳定在海岸出口附近。
            for (var i = 0; i < 100; i++)
            {
                var changed = false;
                foreach (MapCorner q in corners)
                {
                    if (!q.ocean && !q.coast && !q.watershed.coast)
                    {
                        var r = q.downslope.watershed;
                        if (!r.ocean) q.watershed = r;
                        changed = true;
                    }
                }

                if (!changed) break;
            }

            // 顺便统计每个流域终点被多少角点汇入。
            foreach (var q in corners)
            {
                var r = q.watershed;
                r.watershed_size = 1 + r.watershed_size;
            }
        }

        private void CreateRivers()
        {
            // 从若干中高地角点出发，沿 downslope 一路向海岸或内陆水体走。
            // 河流允许终止在湖泊，不再强制一律汇入与边界连通的海洋。
            for (var i = 0; i < (Width + Height) / 4; i++)
            {
                var q = corners[random.NextInt(0, corners.Count - 1)];
                if (q.ocean || q.coast || q.water || q.elevation < 0.12f || q.elevation > 0.3f) continue;
                while (!q.coast && !q.water)
                {
                    if (q == q.downslope)
                    {
                        break;
                    }

                    var edge = lookupEdgeFromCorner(q, q.downslope);
                    if (edge == null)
                    {
                        break;
                    }

                    edge.river = edge.river + 1;
                    q.river++;
                    q.downslope.river++;
                    q = q.downslope;
                }
            }
        }

        private void AssignCornerMoisture()
        {
            // 湿度从淡水源开始传播：
            // 湖泊和河流是扩散源，海洋只在最后直接赋满，不参与内陆扩散。
            var queue = new Queue<MapCorner>();
            foreach (MapCorner q in corners)
            {
                if ((q.water || q.river > 0) && !q.ocean)
                {
                    q.moisture = q.river > 0 ? math.min(3.0f, (0.2f * q.river)) : 1.0f;
                    queue.Enqueue(q);
                }
                else
                {
                    q.moisture = 0;
                }
            }

            // 每经过一层邻接，湿度按 0.9 衰减。
            while (queue.Count > 0)
            {
                var q = queue.Dequeue();

                foreach (var r in q.adjacent)
                {
                    var newMoisture = q.moisture * 0.9f;
                    if (newMoisture > r.moisture)
                    {
                        r.moisture = newMoisture;
                        queue.Enqueue(r);
                    }
                }
            }

            // 海洋和海岸统一视为最高湿度。
            foreach (MapCorner q in corners)
            {
                if (q.ocean || q.coast)
                {
                    q.moisture = 1.0f;
                }
            }
        }

        private void AssignPolygonMoisture()
        {
            // 多边形湿度取角点湿度平均值，并顺手把异常值截到 1。
            foreach (MapCenter p in centers)
            {
                var sumMoisture = 0.0f;
                foreach (MapCorner q in p.corners)
                {
                    if (q.moisture > 1.0)
                        q.moisture = 1.0f;
                    sumMoisture += q.moisture;
                }

                p.moisture = sumMoisture / p.corners.Count;
            }
        }

        private void AssignCornerTemperature()
        {
            float coastalScale = math.max(1f, math.min(Width, Height) * 0.5f);
            foreach (MapCorner q in corners)
            {
                float normalizedY = Height <= 0 ? 0.5f : math.saturate(q.point.y / Height);
                float latitudeHeat = 1f - math.abs(normalizedY * 2f - 1f);
                float drynessHeat = 1f - math.saturate(q.moisture);
                float coastalCooling = (1f - GetEdgeDistance01(q.point, coastalScale)) * 0.08f;
                float waterCooling = q.water ? 0.06f : 0f;
                float noise01 = SampleNoise01(q.point, _temperatureNoiseOffset, 0.018f, 3);

                q.temperature = math.saturate(latitudeHeat * 0.62f +
                    drynessHeat * 0.16f +
                    noise01 * 0.22f -
                    coastalCooling -
                    waterCooling);
            }
        }

        private void AssignPolygonTemperature()
        {
            foreach (MapCenter p in centers)
            {
                float sumTemperature = 0f;
                foreach (MapCorner q in p.corners)
                {
                    sumTemperature += q.temperature;
                }

                p.temperature = sumTemperature / p.corners.Count;
            }
        }

        public MapEdge lookupEdgeFromCenter(MapCenter p, MapCenter r)
        {
            // 查找两个相邻多边形共享的那条边。
            foreach (var edge in p.borders)
            {
                if (edge.d0 == r || edge.d1 == r)
                    return edge;
            }

            return null;
        }

        public MapEdge lookupEdgeFromCorner(MapCorner q, MapCorner s)
        {
            // 查找两个相邻角点之间的那条边。
            foreach (var edge in q.protrudes)
            {
                if (edge.v0 == s || edge.v1 == s)
                    return edge;
            }

            return null;
        }

        private void RedistributeMoisture()
        {
            // 把内陆湿度重新拉伸到 0~1，保证 biome 划分时能覆盖完整区间。
            List<MapCorner> locations = LandCorners;
            if (locations.Count == 0)
            {
                return;
            }

            locations.Sort((a, b) => a.moisture.CompareTo(b.moisture));

            if (locations.Count == 1)
            {
                locations[0].moisture = 0.5f;
                return;
            }

            for (var i = 0; i < locations.Count; i++)
            {
                locations[i].moisture = (float)i / (locations.Count - 1);
            }
        }

        static Biome GetBiome(MapCenter p)
        {
            // biome 分类完全由三类信息决定：
            // 1. 是否是海洋/水域/海岸
            // 2. 温度区间
            // 3. 湿度区间
            if (p.ocean)
            {
                return Biome.Ocean;
            }

            if (p.water)
            {
                if (p.temperature < 0.18f) return Biome.Ice;
                if (p.moisture > 0.85f && p.temperature > 0.45f) return Biome.Marsh;
                return Biome.Lake;
            }

            if (p.coast)
            {
                return Biome.Beach;
            }

            if (p.temperature < 0.14f)
            {
                if (p.moisture > 0.55f) return Biome.Snow;
                if (p.moisture > 0.28f) return Biome.Tundra;
                return Biome.Bare;
            }

            if (p.temperature < 0.28f)
            {
                if (p.moisture < 0.18f) return Biome.Bare;
                if (p.moisture < 0.42f) return Biome.Tundra;
                if (p.moisture < 0.72f) return Biome.Taiga;
                return Biome.Snow;
            }

            if (p.temperature < 0.45f)
            {
                if (p.moisture < 0.16f) return Biome.TemperateDesert;
                if (p.moisture < 0.38f) return Biome.Shrubland;
                if (p.moisture < 0.68f) return Biome.Taiga;
                return Biome.TemperateRainForest;
            }

            if (p.temperature < 0.68f)
            {
                if (p.moisture < 0.14f) return Biome.TemperateDesert;
                if (p.moisture < 0.34f) return Biome.Grassland;
                if (p.moisture < 0.7f) return Biome.TemperateDeciduousForest;
                return Biome.TemperateRainForest;
            }

            if (p.moisture < 0.1f) return Biome.Scorched;
            if (p.moisture < 0.26f) return Biome.SubtropicalDesert;
            if (p.moisture < 0.52f) return Biome.Grassland;
            if (p.moisture < 0.78f) return Biome.TropicalSeasonalForest;
            return Biome.TropicalRainForest;
        }

        private bool IsOceanBand(float2 point)
        {
            float oceanBandSize = math.max(2f, math.min(Width, Height) * 0.12f);
            float rightDistance = Width - point.x;
            if (rightDistance <= oceanBandSize)
            {
                return true;
            }

            float normalizedY = Height <= 0 ? 0.5f : point.y / Height;
            return rightDistance <= math.max(oceanBandSize, Width * 0.34f)
                && math.abs(normalizedY - 0.5f) <= 0.22f;
        }

        private float GetEdgeDistance01(float2 point, float scale)
        {
            return math.saturate(GetEdgeDistance(point) / scale);
        }

        private float GetEdgeDistance(float2 point)
        {
            return math.min(math.min(point.x, Width - point.x), math.min(point.y, Height - point.y));
        }

        private static float SampleSignedNoise(float2 point, float2 offset, float scale, int octave)
        {
            return Perlin.Fbm(point * scale + offset, octave);
        }

        private static float SampleNoise01(float2 point, float2 offset, float scale, int octave)
        {
            return math.saturate(SampleSignedNoise(point, offset, scale, octave) * 0.5f + 0.5f);
        }

        public static IEnumerable<float2> RelaxPoints(IEnumerable<float2> startingPoints, float width, float height)
        {
            // Lloyd Relaxation:
            // 先求每个点对应的 Voronoi 区域，再把点移动到区域质心，
            // 从而让采样点分布更均匀。
            ET.Voronoi v =
                new ET.Voronoi(startingPoints.ToList(), null, new RectangleF(0, 0, width, height));
            foreach (var point in startingPoints)
            {
                var region = v.Region(point);
                point.Set(0, 0);
                foreach (var r in region)
                    point.Set(point.x + r.x, point.y + r.y);

                point.Set(point.x / region.Count, point.y / region.Count);
                yield return point;
            }
        }
    }
}
