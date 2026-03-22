using System.Collections.Generic;
using RectangleF = ET.Geometry.RectangleF;
using Unity.Mathematics;

namespace ET
{
    [ComponentOf(typeof(DrawCarpet))]
    public partial class MapLogic : Entity, IAwake
    {
        public readonly List<float3> Vertices = new List<float3>();
        public readonly List<float2> UV = new List<float2>();
        public readonly List<float2> UV2 = new List<float2>();
        public readonly List<int> Triangles = new List<int>();
        public readonly Dictionary<int2, MapNode> Map = new Dictionary<int2, MapNode>();
        public readonly float2[] TileUV = new float2[4];
        public EntityRef<Brush> Brush;
    }
}
