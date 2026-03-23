using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public partial class GenMap : Entity, IAwake
    {
        public Texture2D TxtTexture;
        public int Width = 400;
        public int Height = 400;
        public int RenderWidth = 50;
        public int RenderHeight = 50;
        public int TxtWidth = 200;
        public int TxtHeight = 200;
        public int PointNum = 200;
        public bool IsLake = true;
        public uint MapSeed = 1;
        public EntityRef<DrawMap> DrawMap;
        public BiomeMap BiomeMap;
    }
}
