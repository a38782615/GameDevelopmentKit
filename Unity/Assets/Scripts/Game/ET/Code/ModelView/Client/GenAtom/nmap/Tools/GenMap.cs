
using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EnableClass]
    public class GenMap
    {
        private Texture2D _txtTexture;
        private int Width = 800;
        private int Height = 600;
        private int _txtWidth = 400;
        private int _txtHeight = 200;
        private int _pointNum = 1000;
        private bool _isLake = true;
        private uint MapSeed = 1;
        BiomeMap biomeMap;
        public GenMap()
        {
            biomeMap = new BiomeMap(new float2(Width, Height));
            biomeMap.SetPointNum(_pointNum);
            biomeMap.Init(MapSeed, CheckIsland);
            //扰乱边缘
            NoisyEdges noisyEdge = new NoisyEdges(MapSeed);
            noisyEdge.BuildNoisyEdges(biomeMap);

            var drawmap = new DrawMap();
            drawmap.View = GameObject.Find("Map");
            drawmap.Init();
            drawmap.GenMap(biomeMap);
        }
        public bool CheckIsland(float2 q)
        {
            int x = Convert.ToInt32(q.x / Width * _txtWidth);
            int y = Convert.ToInt32(q.y / Height * _txtHeight);
            Color tColor = _txtTexture.GetPixel(x, y);
            bool isLand = false;
            if (_isLake)
                isLand = tColor != Color.white;
            else
                isLand = tColor == Color.white;
            return isLand;
        }

    }
}