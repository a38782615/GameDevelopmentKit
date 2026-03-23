using System.Collections.Generic;
using RectangleF = ET.Geometry.RectangleF;
using Unity.Mathematics;

namespace ET
{
    [FriendOf(typeof(MapLogic))]
    [FriendOf(typeof(Brush))]
    [EntitySystemOf(typeof(MapLogic))]
    public static partial class MapLogicSystem
    {
        [EntitySystem]
        private static void Awake(this MapLogic self)
        {
        }

        public static void Init(this MapLogic self)
        {
            self.Map.Clear();
            self.Vertices.Clear();
            self.Triangles.Clear();
            self.UV.Clear();
            self.UV2.Clear();

            Brush brush = self.Brush.As();
            if (brush == null)
            {
                brush = self.AddChild<Brush>();
                self.Brush = brush;
            }

            brush.Init();
        }

        public static void Clear(this MapLogic self)
        {
            self.Vertices.Clear();
            self.Triangles.Clear();
            self.UV.Clear();
            self.UV2.Clear();
        }

        public static void CreateMap(this MapLogic self)
        {
            Brush brush = self.Brush.As();
            if (brush == null)
            {
                return;
            }

            foreach (KeyValuePair<int2, MapNode> pair in self.Map)
            {
                MapNode item = pair.Value;
                int x = item.Pos.x;
                int y = item.Pos.y;
                if (!self.HasNode(x, y))
                {
                    continue;
                }

                int mask = self.GetMaskFromMap(x, y);
                int coverId = Brush.MaskDic[mask];
                int mainId = UVTileMain.GetId(new int2(x, y));
                self.DrawOne(
                    new RectangleF(item.WorldPosition, item.WorldSize),
                    brush.UV2Map[mainId].uvRect,
                    brush.UVMap[coverId].uvRect);
            }
        }

        private static void DrawOne(this MapLogic self, RectangleF posRect, RectangleF tileUv0, RectangleF tileUv1)
        {
            float px0 = posRect.Left;
            float py0 = posRect.Bottom;
            float px1 = posRect.Right;
            float py1 = posRect.Top;

            int vertexIdx = self.Vertices.Count;
            self.Vertices.Add(new float3(px0, py0, 0));
            self.Vertices.Add(new float3(px1, py0, 0));
            self.Vertices.Add(new float3(px0, py1, 0));
            self.Vertices.Add(new float3(px1, py1, 0));
            self.Triangles.Add(vertexIdx + 0);
            self.Triangles.Add(vertexIdx + 1);
            self.Triangles.Add(vertexIdx + 2);
            self.Triangles.Add(vertexIdx + 2);
            self.Triangles.Add(vertexIdx + 1);
            self.Triangles.Add(vertexIdx + 3);

            float u00 = tileUv0.Left;
            float v00 = tileUv0.Bottom;
            float u01 = tileUv0.Right;
            float v01 = tileUv0.Top;
            self.TileUV[0] = new float2(u00, v00);
            self.TileUV[1] = new float2(u01, v00);
            self.TileUV[2] = new float2(u00, v01);
            self.TileUV[3] = new float2(u01, v01);
            for (int i = 0; i < 4; ++i)
            {
                self.UV.Add(self.TileUV[i]);
            }

            float u10 = tileUv1.Left;
            float v10 = tileUv1.Bottom;
            float u11 = tileUv1.Right;
            float v11 = tileUv1.Top;
            self.TileUV[0] = new float2(u10, v10);
            self.TileUV[1] = new float2(u11, v10);
            self.TileUV[2] = new float2(u10, v11);
            self.TileUV[3] = new float2(u11, v11);
            for (int i = 0; i < 4; ++i)
            {
                self.UV2.Add(self.TileUV[i]);
            }
        }

        private static bool HasNode(this MapLogic self, int x, int y)
        {
            return self.Map.TryGetValue(new int2(x, y), out _);
        }

        private static int GetMaskFromMap(this MapLogic self, int x, int y)
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
                        currentMask = self.HasNode(xx, yy) ? Brush.MaskI[maskId] : 0;
                    }
                    else
                    {
                        currentMask = self.HasNode(xx, yy) && self.HasNode(xx, y) && self.HasNode(x, yy)
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
