using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    [FriendOf(typeof(DrawMap))]
    [FriendOf(typeof(DrawCarpet))]
    [EntitySystemOf(typeof(DrawMap))]
    public static partial class DrawMapSystem
    {
        [EntitySystem]
        private static void Awake(this DrawMap self)
        {
        }

        public static async UniTask InitAsync(this DrawMap self)
        {
            self.Grounds.Clear();

            int childCount = self.View.transform.childCount;
            if (childCount <= 0)
            {
                Log.Error($"nmap draw map init failed, no carpet child under view: {self.View?.name}");
                return;
            }

            for (int i = 0; i < childCount; i++)
            {
                DrawCarpet carpet = self.AddChild<DrawCarpet>();
                carpet.View = self.View.transform.GetChild(i).gameObject;
                self.Grounds.Add(carpet);
                await carpet.InitAsync(i);
            }
        }

        public static void GenMap(this DrawMap self, BiomeMap map, int renderWidth, int renderHeight)
        {
            foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.Clear();
            }

            self.Map.Clear();

            List<MapCenter> centers = map.MapGraph.centers;
            float logicWidth = map.MapGraph.Width;
            float logicHeight = map.MapGraph.Height;
            float logicToRenderX = renderWidth / logicWidth;
            float logicToRenderY = renderHeight / logicHeight;
            float renderCellWidth = logicWidth / renderWidth;
            float renderCellHeight = logicHeight / renderHeight;
            float worldCellWidth = logicWidth * UVTileCover.cellSize / renderWidth;
            float worldCellHeight = logicHeight * UVTileCover.cellSize / renderHeight;
            float worldOriginX = -renderWidth * worldCellWidth * 0.5f;
            float worldOriginY = -renderHeight * worldCellHeight * 0.5f;
            int mainTileCount = UVTileMain.TileCount * UVTileMain.TileCount;
            foreach (MapCenter center in centers)
            {
                self.RasterizeCenter(center, renderWidth, renderHeight, logicToRenderX, logicToRenderY, renderCellWidth,
                    renderCellHeight, worldCellWidth, worldCellHeight, worldOriginX, worldOriginY, mainTileCount);
            }

            self.FillRasterizationGaps(centers, renderWidth, renderHeight, renderCellWidth, renderCellHeight, worldCellWidth,
                worldCellHeight, worldOriginX, worldOriginY, mainTileCount);

            foreach (MapNode node in self.Map.Values)
            {
                foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
                {
                    DrawCarpet carpet = carpetRef.As();
                    carpet?.Set(self.IsGround, node);
                }
            }

            foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.GenMap();
            }
        }

        private static void RasterizeCenter(this DrawMap self, MapCenter center, int renderWidth, int renderHeight, float logicToRenderX,
        float logicToRenderY, float renderCellWidth, float renderCellHeight, float worldCellWidth, float worldCellHeight,
        float worldOriginX, float worldOriginY, int mainTileCount)
        {
            if (center == null || center.corners == null || center.corners.Count < 3)
            {
                return;
            }

            float2 min = center.corners[0].point;
            float2 max = min;
            for (int i = 1; i < center.corners.Count; i++)
            {
                float2 corner = center.corners[i].point;
                min = math.min(min, corner);
                max = math.max(max, corner);
            }

            int startX = math.max(0, (int)math.floor(min.x * logicToRenderX) - 1);
            int endX = math.min(renderWidth - 1, (int)math.ceil(max.x * logicToRenderX));
            int startY = math.max(0, (int)math.floor(min.y * logicToRenderY) - 1);
            int endY = math.min(renderHeight - 1, (int)math.ceil(max.y * logicToRenderY));
            for (int x = startX; x <= endX; x++)
            {
                float sampleX = (x + 0.5f) * renderCellWidth;
                for (int y = startY; y <= endY; y++)
                {
                    float sampleY = (y + 0.5f) * renderCellHeight;
                    if (!ContainsPoint(center, sampleX, sampleY))
                    {
                        continue;
                    }

                    MapNode node = self.BuildNode(center, x, y, sampleX, sampleY, worldCellWidth, worldCellHeight,
                        worldOriginX, worldOriginY, renderCellWidth, renderCellHeight, mainTileCount);
                    self.UpsertNode(node, sampleX, sampleY);
                }
            }
        }

        private static void FillRasterizationGaps(this DrawMap self, List<MapCenter> centers, int renderWidth, int renderHeight,
        float renderCellWidth, float renderCellHeight, float worldCellWidth, float worldCellHeight,
        float worldOriginX, float worldOriginY, int mainTileCount)
        {
            for (int x = 0; x < renderWidth; x++)
            {
                float sampleX = (x + 0.5f) * renderCellWidth;
                for (int y = 0; y < renderHeight; y++)
                {
                    int2 pos = new int2(x, y);
                    if (self.Map.ContainsKey(pos))
                    {
                        continue;
                    }

                    float sampleY = (y + 0.5f) * renderCellHeight;
                    MapCenter nearestCenter = FindNearestCenter(centers, sampleX, sampleY);
                    if (nearestCenter == null)
                    {
                        continue;
                    }

                    MapNode node = self.BuildNode(nearestCenter, x, y, sampleX, sampleY, worldCellWidth, worldCellHeight,
                        worldOriginX, worldOriginY, renderCellWidth, renderCellHeight, mainTileCount);
                    self.UpsertNode(node, sampleX, sampleY);
                }
            }
        }

        private static void UpsertNode(this DrawMap self, MapNode node, float sampleX, float sampleY)
        {
            int2 pos = node.Pos;
            if (self.Map.TryGetValue(pos, out MapNode currentNode))
            {
                float currentDistance = math.distancesq(new float2(sampleX, sampleY), currentNode.MapCenter.point);
                float nextDistance = math.distancesq(new float2(sampleX, sampleY), node.MapCenter.point);
                if (currentDistance <= nextDistance)
                {
                    return;
                }
            }

            self.Map[pos] = node;
        }

        private static MapNode BuildNode(this DrawMap self, MapCenter center, int x, int y, float sampleX, float sampleY,
        float worldCellWidth, float worldCellHeight, float worldOriginX, float worldOriginY, float renderCellWidth,
        float renderCellHeight, int mainTileCount)
        {
            float2 samplePoint = new float2(sampleX, sampleY);
            MapCenter secondaryCenter = null;
            MapEdge boundaryEdge = null;
            float edgeDistance = float.MaxValue;
            foreach (MapEdge edge in center.borders)
            {
                if (edge == null || edge.v0 == null || edge.v1 == null)
                {
                    continue;
                }

                MapCenter otherCenter = GetOtherCenter(edge, center);
                if (otherCenter == null)
                {
                    continue;
                }

                float distance = DistancePointToSegment(samplePoint, edge.v0.point, edge.v1.point);
                if (distance >= edgeDistance)
                {
                    continue;
                }

                edgeDistance = distance;
                boundaryEdge = edge;
                secondaryCenter = otherCenter;
            }

            MapCorner boundaryCorner = null;
            float cornerDistance = float.MaxValue;
            foreach (MapCorner corner in center.corners)
            {
                if (corner == null)
                {
                    continue;
                }

                float distance = math.distance(samplePoint, corner.point);
                if (distance >= cornerDistance)
                {
                    continue;
                }

                cornerDistance = distance;
                boundaryCorner = corner;
            }

            MapTransitionKind transitionKind = ClassifyTransition(center, secondaryCenter, boundaryEdge, boundaryCorner);
            float edgeBlend = secondaryCenter == null ? 0f : ComputeBlend(edgeDistance, GetEdgeBlendWidth(renderCellWidth, renderCellHeight, transitionKind));
            float cornerBlend = boundaryCorner == null ? 0f : ComputeBlend(cornerDistance, GetCornerBlendRadius(renderCellWidth, renderCellHeight, transitionKind));
            int mainTileId = ComputeMainTileId(center, secondaryCenter, x, y, transitionKind, edgeBlend, cornerBlend, mainTileCount);
            return new MapNode
            {
                MapCenter = center,
                SecondaryCenter = secondaryCenter,
                BoundaryEdge = boundaryEdge,
                BoundaryCorner = boundaryCorner,
                EdgeDistance = edgeDistance,
                CornerDistance = cornerDistance,
                EdgeBlend = edgeBlend,
                CornerBlend = cornerBlend,
                MainTileId = mainTileId,
                TransitionKind = transitionKind,
                Pos = new int2(x, y),
                WorldPosition = new float2(worldOriginX + x * worldCellWidth, worldOriginY + y * worldCellHeight),
                WorldSize = new float2(worldCellWidth, worldCellHeight)
            };
        }

        private static MapCenter FindNearestCenter(List<MapCenter> centers, float sampleX, float sampleY)
        {
            MapCenter nearestCenter = null;
            float bestDistance = float.MaxValue;
            float2 samplePoint = new float2(sampleX, sampleY);
            foreach (MapCenter center in centers)
            {
                float distance = math.distancesq(samplePoint, center.point);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                nearestCenter = center;
            }

            return nearestCenter;
        }

        private static bool ContainsPoint(MapCenter center, float x, float y)
        {
            int cornerCount = center.corners.Count;
            for (int i = 0, j = cornerCount - 1; i < cornerCount; j = i, i++)
            {
                float2 from = center.corners[j].point;
                float2 to = center.corners[i].point;
                if (IsPointOnSegment(new float2(x, y), from, to))
                {
                    return true;
                }
            }

            bool oddNodes = false;
            for (int i = 0, j = cornerCount - 1; i < cornerCount; j = i, i++)
            {
                float2 from = center.corners[i].point;
                float2 to = center.corners[j].point;
                bool intersect = (from.y < y && to.y >= y) || (to.y < y && from.y >= y);
                if (!intersect || (from.x > x && to.x > x))
                {
                    continue;
                }

                float crossX = from.x + (y - from.y) / (to.y - from.y) * (to.x - from.x);
                if (crossX < x)
                {
                    oddNodes = !oddNodes;
                }
            }

            return oddNodes;
        }

        private static bool IsPointOnSegment(float2 point, float2 from, float2 to)
        {
            float2 segment = to - from;
            float2 offset = point - from;
            float cross = segment.x * offset.y - segment.y * offset.x;
            if (math.abs(cross) > 0.001f)
            {
                return false;
            }

            float dot = math.dot(offset, segment);
            if (dot < 0)
            {
                return false;
            }

            float squaredLength = math.lengthsq(segment);
            return dot <= squaredLength;
        }

        private static MapCenter GetOtherCenter(MapEdge edge, MapCenter currentCenter)
        {
            if (edge == null)
            {
                return null;
            }

            if (edge.d0 == currentCenter)
            {
                return edge.d1;
            }

            if (edge.d1 == currentCenter)
            {
                return edge.d0;
            }

            return null;
        }

        private static float DistancePointToSegment(float2 point, float2 start, float2 end)
        {
            float2 segment = end - start;
            float segmentLengthSq = math.lengthsq(segment);
            if (segmentLengthSq <= 0.0001f)
            {
                return math.distance(point, start);
            }

            float t = math.saturate(math.dot(point - start, segment) / segmentLengthSq);
            float2 projection = start + segment * t;
            return math.distance(point, projection);
        }

        private static float ComputeBlend(float distance, float blendWidth)
        {
            if (blendWidth <= 0.0001f || distance == float.MaxValue)
            {
                return 0f;
            }

            return math.saturate(1f - distance / blendWidth);
        }

        private static float GetEdgeBlendWidth(float renderCellWidth, float renderCellHeight, MapTransitionKind transitionKind)
        {
            float baseWidth = math.max(renderCellWidth, renderCellHeight);
            return transitionKind switch
            {
                MapTransitionKind.WaterCoast => baseWidth * 2.6f,
                MapTransitionKind.WaterInner => baseWidth * 2.2f,
                MapTransitionKind.VegetationEdge => baseWidth * 1.75f,
                MapTransitionKind.ColdEdge => baseWidth * 1.6f,
                MapTransitionKind.DryEdge => baseWidth * 1.45f,
                MapTransitionKind.TerrainEdge => baseWidth * 1.25f,
                _ => baseWidth
            };
        }

        private static float GetCornerBlendRadius(float renderCellWidth, float renderCellHeight, MapTransitionKind transitionKind)
        {
            return GetEdgeBlendWidth(renderCellWidth, renderCellHeight, transitionKind) * 0.8f;
        }

        private static MapTransitionKind ClassifyTransition(MapCenter primaryCenter, MapCenter secondaryCenter, MapEdge edge, MapCorner corner)
        {
            if (primaryCenter == null || secondaryCenter == null)
            {
                return corner != null && corner.coast ? MapTransitionKind.WaterCoast : MapTransitionKind.None;
            }

            bool primaryWater = IsWaterBiome(primaryCenter.biome);
            bool secondaryWater = IsWaterBiome(secondaryCenter.biome);
            if (primaryWater != secondaryWater)
            {
                return primaryCenter.ocean || secondaryCenter.ocean || primaryCenter.coast || secondaryCenter.coast
                    ? MapTransitionKind.WaterCoast
                    : MapTransitionKind.WaterInner;
            }

            bool primaryGreen = IsGreenBiome(primaryCenter.biome);
            bool secondaryGreen = IsGreenBiome(secondaryCenter.biome);
            if (primaryGreen != secondaryGreen)
            {
                return MapTransitionKind.VegetationEdge;
            }

            bool primaryCold = IsColdBiome(primaryCenter.biome);
            bool secondaryCold = IsColdBiome(secondaryCenter.biome);
            if (primaryCold != secondaryCold)
            {
                return MapTransitionKind.ColdEdge;
            }

            bool primaryDry = IsDryBiome(primaryCenter.biome);
            bool secondaryDry = IsDryBiome(secondaryCenter.biome);
            if (primaryDry != secondaryDry)
            {
                return MapTransitionKind.DryEdge;
            }

            return edge != null ? MapTransitionKind.TerrainEdge : MapTransitionKind.None;
        }

        private static int ComputeMainTileId(MapCenter primaryCenter, MapCenter secondaryCenter, int x, int y,
        MapTransitionKind transitionKind, float edgeBlend, float cornerBlend, int mainTileCount)
        {
            int hash = primaryCenter != null ? primaryCenter.index * 73856093 : 0;
            hash ^= x * 19349663;
            hash ^= y * 83492791;
            hash ^= ((int)transitionKind + 1) * 265443576;
            hash ^= (int)(math.round(edgeBlend * 10f) * 374761393);
            hash ^= (int)(math.round(cornerBlend * 10f) * 668265263);
            if (secondaryCenter != null)
            {
                hash ^= secondaryCenter.index * 1597334677;
            }

            int positive = hash & int.MaxValue;
            if (mainTileCount <= 0)
            {
                return 0;
            }

            return positive % mainTileCount;
        }

        private static bool IsWaterBiome(Biome biome)
        {
            return biome == Biome.Ocean || biome == Biome.Lake || biome == Biome.Marsh || biome == Biome.Ice;
        }

        private static bool IsGreenBiome(Biome biome)
        {
            return biome == Biome.Grassland
                || biome == Biome.Taiga
                || biome == Biome.Shrubland
                || biome == Biome.TemperateRainForest
                || biome == Biome.TemperateDeciduousForest
                || biome == Biome.TropicalRainForest
                || biome == Biome.TropicalSeasonalForest;
        }

        private static bool IsColdBiome(Biome biome)
        {
            return biome == Biome.Ice || biome == Biome.Snow || biome == Biome.Tundra || biome == Biome.Bare;
        }

        private static bool IsDryBiome(Biome biome)
        {
            return biome == Biome.Beach || biome == Biome.SubtropicalDesert || biome == Biome.TemperateDesert || biome == Biome.Scorched;
        }

        public static bool IsGround(this DrawMap self, DrawCarpet carpet, MapNode node)
        {
            if (carpet.CarType == 0)
            {
                return true;
            }

            if (carpet.CarType == 1)
            {
                return self.IsWater(node);
            }

            if (carpet.CarType == 2)
            {
                return self.IsGrass(node);
            }

            return false;
        }

        public static bool IsWater(this DrawMap self, MapNode node)
        {
            if (IsWaterBiome(node.MapCenter.biome))
            {
                return true;
            }

            if (node.SecondaryCenter == null || !IsWaterBiome(node.SecondaryCenter.biome))
            {
                return false;
            }

            float blend = math.max(node.EdgeBlend, node.CornerBlend * 0.85f);
            return node.TransitionKind == MapTransitionKind.WaterCoast || node.TransitionKind == MapTransitionKind.WaterInner
                ? blend >= 0.58f
                : false;
        }

        public static bool IsGrass(this DrawMap self, MapNode node)
        {
            if (IsWaterBiome(node.MapCenter.biome))
            {
                return false;
            }

            bool primaryGreen = IsGreenBiome(node.MapCenter.biome);
            bool secondaryGreen = node.SecondaryCenter != null && IsGreenBiome(node.SecondaryCenter.biome);
            bool nearWater = node.SecondaryCenter != null && IsWaterBiome(node.SecondaryCenter.biome);
            float blend = math.max(node.EdgeBlend, node.CornerBlend);
            if (primaryGreen)
            {
                return !nearWater || blend < 0.68f;
            }

            if (!secondaryGreen)
            {
                return false;
            }

            return node.TransitionKind == MapTransitionKind.VegetationEdge || node.TransitionKind == MapTransitionKind.TerrainEdge
                ? blend >= 0.72f
                : false;
        }
    }
}
