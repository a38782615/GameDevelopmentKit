using System.Collections.Generic;
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

        public static void Init(this DrawMap self)
        {
            self.Grounds.Clear();

            int childCount = self.View.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                DrawCarpet carpet = self.AddChild<DrawCarpet>();
                carpet.View = self.View.transform.GetChild(i).gameObject;
                self.Grounds.Add(carpet);
                carpet.Init(i);
            }
        }

        public static void GenMap(this DrawMap self, BiomeMap map)
        {
            foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.Clear();
            }

            self.CenterIdxs.Clear();
            self.Map.Clear();
            self.KDTree = new KDTree();
            self.Query = new KDQuery();

            List<MapCenter> centers = map.MapGraph.centers;
            foreach (MapCenter center in centers)
            {
                self.CenterIdxs.Add(new float3(center.point, 0));
            }

            self.KDTree.Build(self.CenterIdxs.ToArray());
            for (int i = 0; i < map.MapGraph.Width; i++)
            {
                for (int j = 0; j < map.MapGraph.Height; j++)
                {
                    float3 point = new float3(i, j, 0);
                    self.QueryResult.Clear();
                    self.Query.KNearest(self.KDTree, point, 1, self.QueryResult);
                    if (self.QueryResult.Count <= 0)
                    {
                        continue;
                    }

                    int centerIndex = self.QueryResult[0];
                    MapCenter center = centers[centerIndex];
                    int2 pos = new int2(i, j);
                    MapNode node = new MapNode
                    {
                        MapCenter = center,
                        Pos = pos
                    };

                    self.Map[pos] = node;
                    foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
                    {
                        DrawCarpet carpet = carpetRef.As();
                        carpet?.Set(self.IsGround, node);
                    }
                }
            }

            foreach (EntityRef<DrawCarpet> carpetRef in self.Grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.GenMap();
            }
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
