using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [FriendOf(typeof(GenMap))]
    [FriendOf(typeof(DrawMap))]
    [EntitySystemOf(typeof(GenMap))]
    public static partial class GenMapSystem
    {
        [EntitySystem]
        private static void Awake(this GenMap self)
        {
        }

        public static void Build(this GenMap self)
        {
            self.BiomeMap = new BiomeMap(new float2(self.Width, self.Height));
            self.BiomeMap.SetPointNum(self.PointNum);
            self.BiomeMap.Init(self.MapSeed, self.CheckIsland);

            NoisyEdges noisyEdge = new NoisyEdges(self.MapSeed);
            noisyEdge.BuildNoisyEdges(self.BiomeMap);

            DrawMap drawMap = self.DrawMap.As();
            if (drawMap == null)
            {
                drawMap = self.AddChild<DrawMap>();
                self.DrawMap = drawMap;
            }

            drawMap.View = GameObject.Find("Map");
            drawMap.Init();
            drawMap.GenMap(self.BiomeMap);
        }

        public static bool CheckIsland(this GenMap self, float2 q)
        {
            int x = Convert.ToInt32(q.x / self.Width * self.TxtWidth);
            int y = Convert.ToInt32(q.y / self.Height * self.TxtHeight);
            Color textureColor = self.TxtTexture.GetPixel(x, y);
            if (self.IsLake)
            {
                return textureColor != Color.white;
            }

            return textureColor == Color.white;
        }
    }
}
