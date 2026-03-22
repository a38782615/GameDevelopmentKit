using System;
using UnityEngine;

namespace ET
{
    [FriendOf(typeof(DrawCarpet))]
    [FriendOf(typeof(MapLogic))]
    [EntitySystemOf(typeof(DrawCarpet))]
    public static partial class DrawCarpetSystem
    {
        [EntitySystem]
        private static void Awake(this DrawCarpet self)
        {
        }

        public static void Init(this DrawCarpet self, int type)
        {
            self.CarType = type;
            self.MainTexture = Resources.Load<Texture2D>($"Sprites/{DrawCarpet.mainNames[type]}");
            self.OverlayTexture = Resources.Load<Texture2D>($"Sprites/{DrawCarpet.overNames[type]}");
            self.MeshFilter = self.View.GetComponent<MeshFilter>();
            self.MeshRenderer = self.View.GetComponent<MeshRenderer>();
            if (self.MeshRenderer != null)
            {
                self.SetSortingLayerId(0);
                self.SetOrderInLayer(type);
            }

            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                mapLogic = self.AddComponent<MapLogic>();
                self.MapLogic = mapLogic;
            }

            mapLogic.Init();
            mapLogic.Clear();

            self.MeshFilter.sharedMesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"{self.View.name}_mesh"
            };
            self.MeshFilter.sharedMesh.Clear();
            if (self.MatPropBlock == null)
            {
                self.MatPropBlock = new MaterialPropertyBlock();
            }

            self.MeshRenderer.GetPropertyBlock(self.MatPropBlock);
            if (self.MainTexture != null)
            {
                self.MatPropBlock.SetTexture("_MainTex", self.MainTexture);
            }

            if (self.OverlayTexture != null)
            {
                self.MatPropBlock.SetTexture("_Texture2DCover", self.OverlayTexture);
            }

            self.MeshRenderer.SetPropertyBlock(self.MatPropBlock);
        }

        public static void GenMap(this DrawCarpet self)
        {
            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                return;
            }

            mapLogic.CreateMap();
            self.Render();
        }

        public static void Clear(this DrawCarpet self)
        {
            MapLogic mapLogic = self.MapLogic.As();
            mapLogic?.Map.Clear();
        }

        public static void Set(this DrawCarpet self, Func<DrawCarpet, MapNode, bool> func, MapNode node)
        {
            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic != null && func.Invoke(self, node))
            {
                mapLogic.Map[node.Pos] = node;
            }
        }

        public static int GetOrderInLayer(this DrawCarpet self)
        {
            return self.MeshRenderer.sortingOrder;
        }

        public static void SetOrderInLayer(this DrawCarpet self, int value)
        {
            self.MeshRenderer.sortingOrder = value;
        }

        public static int GetSortingLayerId(this DrawCarpet self)
        {
            return self.MeshRenderer.sortingLayerID;
        }

        public static void SetSortingLayerId(this DrawCarpet self, int value)
        {
            self.MeshRenderer.sortingLayerID = value;
        }

        public static string GetSortingLayerName(this DrawCarpet self)
        {
            return self.MeshRenderer.sortingLayerName;
        }

        public static void SetSortingLayerName(this DrawCarpet self, string value)
        {
            self.MeshRenderer.sortingLayerName = value;
        }

        private static void Render(this DrawCarpet self)
        {
            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                return;
            }

            Mesh mesh = self.MeshFilter.sharedMesh;
            mesh.SetVertices(DrawUtil.ToList(mapLogic.Vertices, self.Vertices));
            mesh.SetTriangles(mapLogic.Triangles, 0);
            mesh.SetUVs(0, DrawUtil.ToList(mapLogic.UV, self.UV));
            mesh.SetUVs(1, DrawUtil.ToList(mapLogic.UV2, self.UV2));
        }
    }
}
