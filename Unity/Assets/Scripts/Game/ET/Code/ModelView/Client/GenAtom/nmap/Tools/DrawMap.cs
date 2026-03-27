using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET
{
    [ChildOf(typeof(GenMap))]
    public partial class DrawMap : Entity, IAwake
    {
        public GameObject View;
        public readonly List<EntityRef<DrawCarpet>> Grounds = new List<EntityRef<DrawCarpet>>();
        public readonly Dictionary<int2, MapNode> Map = new Dictionary<int2, MapNode>();
        public readonly HashSet<int2> RemovedGrassCells = new HashSet<int2>();
        public int RenderWidth;
        public int RenderHeight;
        public float2 WorldOrigin;
        public float2 WorldCellSize;
    }
}
