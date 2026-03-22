using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    [EnableMethod]
    [FriendOf(typeof(DrawMap))]
    public partial class GenMap : Entity, IAwake
    {
        private Texture2D _txtTexture;
        private int _width = 800;
        private int _height = 600;
        private int _txtWidth = 400;
        private int _txtHeight = 200;
        private int _pointNum = 1000;
        private bool _isLake = true;
        private uint _mapSeed = 1;

        public EntityRef<DrawMap> DrawMap;
        public BiomeMap BiomeMap;

        public void Build()
        {
            this.BiomeMap = new BiomeMap(new float2(this._width, this._height));
            this.BiomeMap.SetPointNum(this._pointNum);
            this.BiomeMap.Init(this._mapSeed, this.CheckIsland);

            NoisyEdges noisyEdge = new NoisyEdges(this._mapSeed);
            noisyEdge.BuildNoisyEdges(this.BiomeMap);

            DrawMap drawMap = this.DrawMap.As();
            if (drawMap == null)
            {
                drawMap = this.AddChild<DrawMap>();
                this.DrawMap = drawMap;
            }

            drawMap.View = GameObject.Find("Map");
            drawMap.Init();
            drawMap.GenMap(this.BiomeMap);
        }

        public bool CheckIsland(float2 q)
        {
            int x = Convert.ToInt32(q.x / this._width * this._txtWidth);
            int y = Convert.ToInt32(q.y / this._height * this._txtHeight);
            Color tColor = this._txtTexture.GetPixel(x, y);
            bool isLand = false;
            if (this._isLake)
            {
                isLand = tColor != Color.white;
            }
            else
            {
                isLand = tColor == Color.white;
            }

            return isLand;
        }
    }
}
