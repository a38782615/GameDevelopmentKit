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
        public KDTree KDTree;
        public KDQuery Query;
        public readonly List<float3> CenterIdxs = new List<float3>();
        public readonly Dictionary<int2, MapNode> Map = new Dictionary<int2, MapNode>();
        public readonly List<int> QueryResult = new List<int>();
    }
}
