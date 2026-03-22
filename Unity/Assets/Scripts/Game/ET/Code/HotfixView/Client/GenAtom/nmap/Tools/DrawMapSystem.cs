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
            foreach (MapCenter center in centers)
            {
                self.RasterizeCenter(center, renderWidth, renderHeight, logicToRenderX, logicToRenderY, renderCellWidth,
                    renderCellHeight, worldCellWidth, worldCellHeight);
            }

            self.FillRasterizationGaps(centers, renderWidth, renderHeight, renderCellWidth, renderCellHeight, worldCellWidth,
                worldCellHeight);

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
        float logicToRenderY, float renderCellWidth, float renderCellHeight, float worldCellWidth, float worldCellHeight)
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

                    self.UpsertNode(center, x, y, sampleX, sampleY, worldCellWidth, worldCellHeight);
                }
            }
        }

        private static void FillRasterizationGaps(this DrawMap self, List<MapCenter> centers, int renderWidth, int renderHeight,
        float renderCellWidth, float renderCellHeight, float worldCellWidth, float worldCellHeight)
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

                    self.UpsertNode(nearestCenter, x, y, sampleX, sampleY, worldCellWidth, worldCellHeight);
                }
            }
        }

        private static void UpsertNode(this DrawMap self, MapCenter center, int x, int y, float sampleX, float sampleY,
        float worldCellWidth, float worldCellHeight)
        {
            int2 pos = new int2(x, y);
            if (self.Map.TryGetValue(pos, out MapNode currentNode))
            {
                float currentDistance = math.distancesq(new float2(sampleX, sampleY), currentNode.MapCenter.point);
                float nextDistance = math.distancesq(new float2(sampleX, sampleY), center.point);
                if (currentDistance <= nextDistance)
                {
                    return;
                }
            }

            self.Map[pos] = new MapNode
            {
                MapCenter = center,
                Pos = pos,
                WorldPosition = new float2(x * worldCellWidth, y * worldCellHeight),
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
            return node.MapCenter.biome == Biome.Ocean
                || node.MapCenter.biome == Biome.Lake
                || node.MapCenter.biome == Biome.TropicalRainForest
                || node.MapCenter.biome == Biome.Ice;
        }

        public static bool IsGrass(this DrawMap self, MapNode node)
        {
            return node.MapCenter.biome == Biome.Grassland;
        }
    }
}
