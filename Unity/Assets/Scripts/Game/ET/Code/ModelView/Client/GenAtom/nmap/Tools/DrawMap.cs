using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ChildOf(typeof(GenMap))]
    [EnableMethod]
    [FriendOf(typeof(DrawCarpet))]
    public partial class DrawMap : Entity, IAwake
    {
        public GameObject View;

        private readonly List<EntityRef<DrawCarpet>> grounds = new List<EntityRef<DrawCarpet>>();
        private KDTree kdTree;
        private KDQuery query;
        private readonly List<float3> centerIdxs = new List<float3>();
        private readonly Dictionary<int2, MapNode> m_map = new Dictionary<int2, MapNode>();
        private readonly List<int> m_queryResult = new List<int>();

        public void Init()
        {
            this.grounds.Clear();

            int childCount = this.View.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                DrawCarpet carpet = this.AddChild<DrawCarpet>();
                carpet.View = this.View.transform.GetChild(i).gameObject;
                this.grounds.Add(carpet);
                carpet.Init(i);
            }
        }

        public void GenMap(BiomeMap map)
        {
            foreach (EntityRef<DrawCarpet> carpetRef in this.grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.Clear();
            }

            this.centerIdxs.Clear();
            this.m_map.Clear();
            this.kdTree = new KDTree();
            this.query = new KDQuery();

            List<MapCenter> centers = map.MapGraph.centers;
            foreach (MapCenter center in centers)
            {
                this.centerIdxs.Add(new float3(center.point, 0));
            }

            this.kdTree.Build(this.centerIdxs.ToArray());
            for (int i = 0; i < map.MapGraph.Width; i++)
            {
                for (int j = 0; j < map.MapGraph.Height; j++)
                {
                    float3 point = new float3(i, j, 0);
                    this.m_queryResult.Clear();
                    this.query.KNearest(this.kdTree, point, 1, this.m_queryResult);
                    if (this.m_queryResult.Count <= 0)
                    {
                        continue;
                    }

                    int centerIndex = this.m_queryResult[0];
                    MapCenter center = centers[centerIndex];
                    int2 pos = new int2(i, j);
                    MapNode node = new MapNode
                    {
                        MapCenter = center,
                        Pos = pos
                    };

                    this.m_map[pos] = node;
                    foreach (EntityRef<DrawCarpet> carpetRef in this.grounds)
                    {
                        DrawCarpet carpet = carpetRef.As();
                        carpet?.Set(this.IsGround, node);
                    }
                }
            }

            foreach (EntityRef<DrawCarpet> carpetRef in this.grounds)
            {
                DrawCarpet carpet = carpetRef.As();
                carpet?.GenMap();
            }
        }

        public bool IsGround(DrawCarpet carpet, MapNode node)
        {
            if (carpet.CarType == 0)
            {
                return true;
            }

            if (carpet.CarType == 1)
            {
                return this.IsWater(node);
            }

            if (carpet.CarType == 2)
            {
                return this.IsGrass(node);
            }

            return false;
        }

        public bool IsWater(MapNode node)
        {
            return node.MapCenter.biome == Biome.Ocean
                || node.MapCenter.biome == Biome.Lake
                || node.MapCenter.biome == Biome.TropicalRainForest
                || node.MapCenter.biome == Biome.Ice;
        }

        public bool IsGrass(MapNode node)
        {
            return node.MapCenter.biome == Biome.Grassland;
        }
    }
}
