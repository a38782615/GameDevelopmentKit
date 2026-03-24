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
        public int RenderWidth = 100;
        public int RenderHeight = 50;
        public int TxtWidth = 400;
        public int TxtHeight = 200;
        public int PointNum = 1000;
        public bool IsLake = true;
        public uint MapSeed = 12;
        public float LakeInlandMaskRange = 0.82f;
        public float LakeCarveThreshold = 0.56f;
        public float LakeCarveStrength = 0.42f;
        public EntityRef<DrawMap> DrawMap;
        public BiomeMap BiomeMap;
    }
}
