using System.Collections.Generic;
using RectangleF = ET.Geometry.RectangleF;
using Unity.Mathematics;

namespace ET
{
    [ChildOf(typeof(DrawCarpet))]
    [EnableMethod]
    [FriendOf(typeof(Brush))]
    public partial class MapLogic : Entity, IAwake
    {
        public readonly List<float3> s_vertices = new List<float3>();
        public readonly List<float2> m_uv = new List<float2>();
        public readonly List<float2> m_uv2 = new List<float2>();
        public readonly List<int> s_triangles = new List<int>();
        public Dictionary<int2, MapNode> Map = new Dictionary<int2, MapNode>();

        private readonly float2[] s_tileUV = new float2[4];
        private EntityRef<Brush> brush;

        public void Init()
        {
            this.Map.Clear();
            this.s_vertices.Clear();
            this.s_triangles.Clear();
            this.m_uv.Clear();
            this.m_uv2.Clear();

            Brush brush = this.brush.As();
            if (brush == null)
            {
                brush = this.AddChild<Brush>();
                this.brush = brush;
            }

            brush.Init();
        }

        public void Clear()
        {
            this.s_vertices.Clear();
            this.s_triangles.Clear();
            this.m_uv.Clear();
            this.m_uv2.Clear();
        }

        public void CreateMap()
        {
            Brush brush = this.brush.As();
            if (brush == null)
            {
                return;
            }

            foreach (KeyValuePair<int2, MapNode> pair in this.Map)
            {
                MapNode item = pair.Value;
                int x = item.Pos.x;
                int y = item.Pos.y;
                if (!this.HasNode(x, y))
                {
                    continue;
                }

                int mask = this.GetMaskFromMap(x, y);
                int coverId = Brush.MaskDic[mask];
                int mainId = UVTileMain.GetId(new int2(x, y));
                this.DrawOne(
                    new RectangleF(x * UVTileCover.cellSize, y * UVTileCover.cellSize, UVTileCover.cellSize, UVTileCover.cellSize),
                    brush.m_uv2Map[mainId].uvRect,
                    brush.m_uvMap[coverId].uvRect);
            }
        }

        private void DrawOne(RectangleF posRect, RectangleF tileUV0, RectangleF tileUV1)
        {
            float px0 = posRect.Left;
            float py0 = posRect.Bottom;
            float px1 = posRect.Right;
            float py1 = posRect.Top;

            int vertexIdx = this.s_vertices.Count;
            this.s_vertices.Add(new float3(px0, py0, 0));
            this.s_vertices.Add(new float3(px1, py0, 0));
            this.s_vertices.Add(new float3(px0, py1, 0));
            this.s_vertices.Add(new float3(px1, py1, 0));
            this.s_triangles.Add(vertexIdx + 3);
            this.s_triangles.Add(vertexIdx + 0);
            this.s_triangles.Add(vertexIdx + 2);
            this.s_triangles.Add(vertexIdx + 0);
            this.s_triangles.Add(vertexIdx + 3);
            this.s_triangles.Add(vertexIdx + 1);

            float u00 = tileUV0.Left;
            float v00 = tileUV0.Bottom;
            float u01 = tileUV0.Right;
            float v01 = tileUV0.Top;
            this.s_tileUV[0] = new float2(u00, v00);
            this.s_tileUV[1] = new float2(u01, v00);
            this.s_tileUV[2] = new float2(u00, v01);
            this.s_tileUV[3] = new float2(u01, v01);
            for (int i = 0; i < 4; ++i)
            {
                this.m_uv.Add(this.s_tileUV[i]);
            }

            float u10 = tileUV1.Left;
            float v10 = tileUV1.Bottom;
            float u11 = tileUV1.Right;
            float v11 = tileUV1.Top;
            this.s_tileUV[0] = new float2(u10, v10);
            this.s_tileUV[1] = new float2(u11, v10);
            this.s_tileUV[2] = new float2(u10, v11);
            this.s_tileUV[3] = new float2(u11, v11);
            for (int i = 0; i < 4; ++i)
            {
                this.m_uv2.Add(this.s_tileUV[i]);
            }
        }

        private bool HasNode(int x, int y)
        {
            return this.Map.TryGetValue(new int2(x, y), out _);
        }

        private int GetMaskFromMap(int x, int y)
        {
            int mask = 0;
            for (int j = -1; j < 2; j++)
            {
                for (int i = -1; i < 2; i++)
                {
                    int xx = x + i;
                    int yy = y + j;
                    int xi = i + 1;
                    int yi = j + 1;
                    int maskId = xi + yi * 3;
                    int currentMask;
                    if (i == 0 || j == 0)
                    {
                        currentMask = this.HasNode(xx, yy) ? Brush.MaskI[maskId] : 0;
                    }
                    else
                    {
                        currentMask = this.HasNode(xx, yy) && this.HasNode(xx, y) && this.HasNode(x, yy)
                            ? Brush.MaskI[maskId]
                            : 0;
                    }

                    mask += currentMask;
                }
            }

            return mask;
        }
    }
}
