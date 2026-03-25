using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

namespace ET
{
    /// <summary>
    /// 按“地貌簇”给 MapGraph 中的多边形中心点分配 biome。
    /// 整体思路不是逐点独立抽卡，而是先挑出一批簇种子，再把附近陆地归并到同一簇，
    /// 这样最终结果会形成连续的大块气候区，而不是高度碎片化的随机拼贴。
    /// </summary>
    public static class MapGraphClusterBiomeSystem
    {
        /// <summary>
        /// 地貌分配主入口。
        /// 调用时机位于 Voronoi / 水体判定完成之后、正式绘制地图之前。
        /// 流程分为四步：
        /// 1. 先把海洋、海岸、内陆水域、陆地拆开。
        /// 2. 对陆地做簇种子采样。
        /// 3. 用“带噪声的最近簇”给陆地赋值，并做少量平滑。
        /// 4. 最后单独处理内陆水域，避免被陆地规则污染。
        /// </summary>
        public static void ApplyClusterBiomes(this MapGraph self, uint seed)
        {
            if (self == null || self.centers == null || self.centers.Count == 0)
            {
                return;
            }

            List<MapCenter> landCenters = new List<MapCenter>();
            List<MapCenter> inlandWaters = new List<MapCenter>();
            foreach (MapCenter center in self.centers)
            {
                if (center == null)
                {
                    continue;
                }

                if (center.ocean)
                {
                    // 海洋直接固定为 Ocean，不参与后续簇计算。
                    center.biome = Biome.Ocean;
                    continue;
                }

                if (center.coast)
                {
                    // 海岸线统一打成 Beach，用来稳定陆海过渡带。
                    center.biome = Biome.Beach;
                    continue;
                }

                if (center.water)
                {
                    // 内陆水域留到最后单独判定，届时会根据纬度和河流情况区分湖泊、沼泽、冰面。
                    inlandWaters.Add(center);
                    continue;
                }

                // 剩余部分视为普通陆地，后面参与簇划分。
                landCenters.Add(center);
            }

            if (landCenters.Count > 0)
            {
                Random random = Random.CreateFromIndex(seed == 0u ? 1u : seed);
                List<(MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)> seeds =
                    BuildClusterSeeds(self, landCenters, ref random);
                AssignLandBiomes(self, landCenters, seeds);
                SmoothLandBiomes(landCenters, 2);
            }

            AssignWaterBiomes(self, inlandWaters);
        }

        private static List<(MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)>
        BuildClusterSeeds(MapGraph self, List<MapCenter> landCenters, ref Random random)
        {
            // 种子数量随陆地规模缓慢增长。
            // sqrt 让大地图不会出现过多簇，0.55f / 6 / 18 则是经验参数，
            // 目标是让地图既有大块地貌，又保留一定变化。
            int seedCount = math.clamp((int)math.round(math.sqrt(landCenters.Count) * 0.55f), 6, 18);
            List<(MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)> result =
                new List<(MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)>(seedCount);
            List<MapCenter> selectedCenters = new List<MapCenter>(seedCount);

            // 第一个种子随机挑选，避免所有地图都从固定角落开始扩散。
            MapCenter firstCenter = landCenters[random.NextInt(0, landCenters.Count)];
            selectedCenters.Add(firstCenter);
            result.Add(CreateClusterSeed(self, firstCenter, ref random));

            while (result.Count < seedCount)
            {
                MapCenter nextCenter = null;
                float bestScore = float.MinValue;
                foreach (MapCenter candidate in landCenters)
                {
                    // 这里使用“离已选种子最远”的思路继续挑点，效果类似带扰动的最远点采样。
                    // 这样能把簇尽量铺开，减少多个簇挤在一小片区域里的情况。
                    float minDistanceSq = float.MaxValue;
                    foreach (MapCenter selected in selectedCenters)
                    {
                        float distanceSq = math.lengthsq(candidate.point - selected.point);
                        if (distanceSq < minDistanceSq)
                        {
                            minDistanceSq = distanceSq;
                        }
                    }

                    // 额外乘一点随机扰动，避免种子分布过于机械对称。
                    float jitter = random.NextFloat(0.9f, 1.15f);
                    float score = minDistanceSq * jitter;
                    if (score <= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    nextCenter = candidate;
                }

                if (nextCenter == null)
                {
                    break;
                }

                selectedCenters.Add(nextCenter);
                result.Add(CreateClusterSeed(self, nextCenter, ref random));
            }

            return result;
        }

        private static (MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)
        CreateClusterSeed(MapGraph self, MapCenter center, ref Random random)
        {
            // 每个簇种子除了持有一个目标 biome，还会保存：
            // 1. DistanceScale：让不同簇的“势力半径”略有差异。
            // 2. NoiseOffsetX / NoiseOffsetY：让同一张噪声图对不同簇呈现不同相位。
            float latitude = GetLatitude01(self, center);
            float waterDistance01 = EstimateWaterDistance01(self, center);
            return (
                center,
                ChooseLandBiome(latitude, waterDistance01, ref random),
                random.NextFloat(0.82f, 1.2f),
                random.NextFloat(7f, 71f),
                random.NextFloat(11f, 97f));
        }

        private static void AssignLandBiomes(
            MapGraph self,
            List<MapCenter> landCenters,
            List<(MapCenter Center, Biome Biome, float DistanceScale, float NoiseOffsetX, float NoiseOffsetY)> seeds)
        {
            if (seeds.Count == 0)
            {
                return;
            }

            float noiseScaleX = math.max(1f, self.Width) * 0.018f;
            float noiseScaleY = math.max(1f, self.Height) * 0.018f;
            foreach (MapCenter center in landCenters)
            {
                // 这里的分配本质上是“带噪声的 Voronoi”：
                // 基础项由距离决定，确保同簇大体连续；
                // 噪声项负责把过直的边界揉碎，避免所有分界线都像尺子切出来。
                int bestSeedIndex = -1;
                float bestScore = float.MaxValue;
                for (int i = 0; i < seeds.Count; i++)
                {
                    (MapCenter seedCenter, _, float distanceScale, float noiseOffsetX, float noiseOffsetY) = seeds[i];
                    float distanceSq = math.lengthsq(center.point - seedCenter.point) * distanceScale;
                    float noise = Perlin.Fbm(
                        new float2(center.point.x * noiseScaleX + noiseOffsetX, center.point.y * noiseScaleY + noiseOffsetY),
                        3);
                    float score = distanceSq - noise * 280f;
                    if (score >= bestScore)
                    {
                        continue;
                    }

                    bestScore = score;
                    bestSeedIndex = i;
                }

                center.biome = bestSeedIndex >= 0 ? seeds[bestSeedIndex].Biome : Biome.Grassland;
            }
        }

        private static void SmoothLandBiomes(List<MapCenter> landCenters, int iterations)
        {
            // 第一步生成的大块分区已经够用了，但边缘仍可能出现 1~2 格的噪点。
            // 这里做少量邻域投票，只在“周围明显多数派”时才改写当前地貌，
            // 这样既能消噪，又不会把原本有意义的细长地带完全抹平。
            Dictionary<MapCenter, Biome> nextBiomes = new Dictionary<MapCenter, Biome>(landCenters.Count);
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                nextBiomes.Clear();
                foreach (MapCenter center in landCenters)
                {
                    Dictionary<Biome, int> counts = new Dictionary<Biome, int>();
                    foreach (MapCenter neighbor in center.neighbors)
                    {
                        if (neighbor == null || neighbor.water || neighbor.ocean || neighbor.coast)
                        {
                            continue;
                        }

                        if (!counts.TryAdd(neighbor.biome, 1))
                        {
                            counts[neighbor.biome]++;
                        }
                    }

                    if (counts.Count == 0)
                    {
                        continue;
                    }

                    KeyValuePair<Biome, int> best = default;
                    bool hasBest = false;
                    foreach (KeyValuePair<Biome, int> pair in counts)
                    {
                        if (hasBest && pair.Value <= best.Value)
                        {
                            continue;
                        }

                        best = pair;
                        hasBest = true;
                    }

                    // 只有邻居中出现足够强的多数派，且显著压过当前类型时才翻转。
                    int currentCount = counts.TryGetValue(center.biome, out int value) ? value : 0;
                    if (hasBest && best.Value >= 3 && best.Value >= currentCount + 2)
                    {
                        nextBiomes[center] = best.Key;
                    }
                }

                foreach ((MapCenter center, Biome biome) in nextBiomes)
                {
                    center.biome = biome;
                }
            }
        }

        private static void AssignWaterBiomes(MapGraph self, List<MapCenter> inlandWaters)
        {
            foreach (MapCenter center in inlandWaters)
            {
                float latitude = GetLatitude01(self, center);
                int riverEdges = 0;
                foreach (MapEdge edge in center.borders)
                {
                    if (edge != null && edge.river > 0)
                    {
                        riverEdges++;
                    }
                }

                // 纬度越靠两极越容易冻结。
                if (latitude > 0.78f)
                {
                    center.biome = Biome.Ice;
                    continue;
                }

                // 靠近赤道且没有明显河流补给时，给它更像湿地 / 沼泽的感觉。
                if (latitude < 0.22f && riverEdges <= 1)
                {
                    center.biome = Biome.Marsh;
                    continue;
                }

                // 其余内陆水域统一按湖泊处理。
                center.biome = Biome.Lake;
            }
        }

        private static float GetLatitude01(MapGraph self, MapCenter center)
        {
            // 返回值不是现实世界经纬度，而是“离赤道有多远”的归一化指标：
            // 地图中线附近接近 0，两端接近 1。
            float normalizedY = self.Height <= 0 ? 0.5f : math.saturate(center.point.y / self.Height);
            return math.abs(normalizedY * 2f - 1f);
        }

        private static float EstimateWaterDistance01(MapGraph self, MapCenter center)
        {
            // 用有限层数 BFS 估算“离任意水域有多远”。
            // 我们不追求真实距离，只需要一个足够稳定的湿润度近似值供 biome 采样使用。
            int maxDepth = 5;
            Queue<MapCenter> queue = new Queue<MapCenter>();
            HashSet<MapCenter> visited = new HashSet<MapCenter>();
            queue.Enqueue(center);
            visited.Add(center);

            int depth = 0;
            while (queue.Count > 0 && depth <= maxDepth)
            {
                int layerCount = queue.Count;
                for (int i = 0; i < layerCount; i++)
                {
                    MapCenter current = queue.Dequeue();
                    if (current.water || current.ocean || current.coast)
                    {
                        return depth / (float)maxDepth;
                    }

                    foreach (MapCenter neighbor in current.neighbors)
                    {
                        if (neighbor == null || !visited.Add(neighbor))
                        {
                            continue;
                        }

                        queue.Enqueue(neighbor);
                    }
                }

                depth++;
            }

            // 超过搜索深度仍未遇到水，就认为它属于非常内陆的干燥区域。
            return 1f;
        }

        private static Biome ChooseLandBiome(float latitude, float waterDistance01, ref Random random)
        {
            // 这是一个简化版气候查表：
            // latitude 控制冷热，waterDistance01 控制干湿，roll 用来在同一气候带内做随机细分。
            // 这样同纬度区域会倾向于相近地貌，但不会完全一成不变。
            float roll = random.NextFloat();
            if (latitude > 0.72f)
            {
                if (roll < 0.22f) return Biome.Snow;
                if (roll < 0.52f) return Biome.Tundra;
                if (roll < 0.78f) return Biome.Taiga;
                return Biome.Bare;
            }

            if (latitude > 0.48f)
            {
                if (waterDistance01 < 0.28f)
                {
                    if (roll < 0.4f) return Biome.TemperateDeciduousForest;
                    if (roll < 0.7f) return Biome.Taiga;
                    return Biome.Grassland;
                }

                if (roll < 0.24f) return Biome.Shrubland;
                if (roll < 0.5f) return Biome.Grassland;
                if (roll < 0.78f) return Biome.TemperateDesert;
                return Biome.Bare;
            }

            if (latitude > 0.24f)
            {
                if (waterDistance01 < 0.22f)
                {
                    if (roll < 0.34f) return Biome.TemperateRainForest;
                    if (roll < 0.68f) return Biome.TemperateDeciduousForest;
                    return Biome.Grassland;
                }

                if (roll < 0.22f) return Biome.Grassland;
                if (roll < 0.44f) return Biome.Shrubland;
                if (roll < 0.68f) return Biome.TemperateDesert;
                if (roll < 0.86f) return Biome.SubtropicalDesert;
                return Biome.Scorched;
            }

            if (waterDistance01 < 0.24f)
            {
                if (roll < 0.35f) return Biome.TropicalRainForest;
                if (roll < 0.72f) return Biome.TropicalSeasonalForest;
                return Biome.Grassland;
            }

            if (roll < 0.24f) return Biome.Grassland;
            if (roll < 0.52f) return Biome.TropicalSeasonalForest;
            if (roll < 0.8f) return Biome.SubtropicalDesert;
            return Biome.Scorched;
        }
    }
}
