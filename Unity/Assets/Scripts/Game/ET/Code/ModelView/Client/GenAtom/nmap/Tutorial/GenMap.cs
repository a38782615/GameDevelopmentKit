
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [EnableClass]
    public class GenMap
    {
        private Texture2D _txtTexture;
        const int TextureScale = 20;
        private int Width = 100;
        private int Height = 50;
        private int _pointNum = 1000;
        private bool _isLake = true;
        private uint MapSeed = 1;
        Unity.Mathematics.Random random;
        public GenMap(Unity.Mathematics.Random r)
        {
            random = r;
            _txtTexture = GetTextTexture();

            BiomeMap biomeMap = new BiomeMap(new float2(Width, Height));
            biomeMap.SetPointNum(_pointNum);
            biomeMap.Init(MapSeed, CheckIsland());
            //扰乱边缘
            NoisyEdges noisyEdge = new NoisyEdges(r);
            noisyEdge.BuildNoisyEdges(biomeMap);

            var mapGo = GameObject.Find("Map");
            var drawmap = new DrawMap(mapGo);
            drawmap.Init();
            drawmap.GenMap(biomeMap);
        }
        public System.Func<float2, bool> CheckIsland()
        {
            System.Func<float2, bool> inside = q =>
            {
                int x = System.Convert.ToInt32(q.x / Width * _txtWidth);
                int y = System.Convert.ToInt32(q.y / Height * _txtHeight);
                Color tColor = _txtTexture.GetPixel(x, y);
                bool isLand = false;
                if (_isLake)
                    isLand = tColor != Color.white;
                else
                    isLand = tColor == Color.white;
                return isLand;
            };
            return inside;
        }

        private int _txtWidth = 400;
        private int _txtHeight = 200;
        private Texture2D GetTextTexture()
        {
            Texture2D output = new Texture2D(_txtWidth, _txtHeight);
            RenderTexture renderTexture = new RenderTexture(_txtWidth, _txtHeight, 24);
            RenderTexture.active = renderTexture;
            Camera myCamera = Camera.main;
            myCamera.orthographic = true;
            myCamera.orthographicSize = 100;
            myCamera.targetTexture = renderTexture;
            myCamera.Render();

            output.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            output.Apply();
            RenderTexture.active = null;

            //_image.texture = renderTexture;
            myCamera.targetTexture = null;
            myCamera.orthographic = false;
            myCamera.Render();
            return output;
        }
    }
}