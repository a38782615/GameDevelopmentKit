using System;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ComponentOf(typeof(Scene))]
    public partial class GenMap : Entity, IAwake
    {
        public Texture2D TxtTexture;
        public int Width = 800;
        public int Height = 600;
        public int TxtWidth = 400;
        public int TxtHeight = 200;
        public int PointNum = 1000;
        public bool IsLake = true;
        public uint MapSeed = 1;
        public EntityRef<DrawMap> DrawMap;
        public BiomeMap BiomeMap;
    }
}
