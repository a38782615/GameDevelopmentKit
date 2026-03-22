using System;
using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    [ChildOf(typeof(DrawMap))]
    [EnableMethod]
    [FriendOf(typeof(MapLogic))]
    public partial class DrawCarpet : Entity, IAwake
    {
        [StaticField]
        public static string[] mainNames = { "noise_rocky", "Ground_noise_water_shallow", "forest_ground_noise" };

        [StaticField]
        public static string[] overNames = { "blocky", "water", "grass" };

        public MeshRenderer m_meshRenderer;
        public MeshFilter meshFilter;
        public Texture2D mainTexture;
        public Texture2D overlayTexture;
        public GameObject View;
        public int CarType;

        private MaterialPropertyBlock m_matPropBlock;
        private EntityRef<MapLogic> m_mapLogic;
        private readonly List<Vector3> s_vertices = new List<Vector3>();
        private readonly List<Vector2> m_uv = new List<Vector2>();
        private readonly List<Vector2> m_uv2 = new List<Vector2>();

        public int OrderInLayer
        {
            get { return this.m_meshRenderer.sortingOrder; }
            set { this.m_meshRenderer.sortingOrder = value; }
        }

        public int SortingLayerID
        {
            get { return this.m_meshRenderer.sortingLayerID; }
            set { this.m_meshRenderer.sortingLayerID = value; }
        }

        public string SortingLayerName
        {
            get { return this.m_meshRenderer.sortingLayerName; }
            set { this.m_meshRenderer.sortingLayerName = value; }
        }

        public void Init(int type)
        {
            this.CarType = type;
            this.mainTexture = Resources.Load<Texture2D>($"Sprites/{mainNames[type]}");
            this.overlayTexture = Resources.Load<Texture2D>($"Sprites/{overNames[type]}");
            this.meshFilter = this.View.GetComponent<MeshFilter>();
            this.m_meshRenderer = this.View.GetComponent<MeshRenderer>();
            if (this.m_meshRenderer != null)
            {
                this.SortingLayerID = 0;
                this.OrderInLayer = type;
            }

            MapLogic mapLogic = this.m_mapLogic.As();
            if (mapLogic == null)
            {
                mapLogic = this.AddChild<MapLogic>();
                this.m_mapLogic = mapLogic;
            }

            mapLogic.Init();
            mapLogic.Clear();

            this.meshFilter.sharedMesh = new Mesh
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = $"{this.View.name}_mesh"
            };
            this.meshFilter.sharedMesh.Clear();
            if (this.m_matPropBlock == null)
            {
                this.m_matPropBlock = new MaterialPropertyBlock();
            }

            this.m_meshRenderer.GetPropertyBlock(this.m_matPropBlock);
            if (this.mainTexture != null)
            {
                this.m_matPropBlock.SetTexture("_MainTex", this.mainTexture);
            }

            if (this.overlayTexture != null)
            {
                this.m_matPropBlock.SetTexture("_Texture2DCover", this.overlayTexture);
            }

            this.m_meshRenderer.SetPropertyBlock(this.m_matPropBlock);
        }

        public void GenMap()
        {
            MapLogic mapLogic = this.m_mapLogic.As();
            if (mapLogic == null)
            {
                return;
            }

            mapLogic.CreateMap();
            this.Render();
        }

        public void Clear()
        {
            MapLogic mapLogic = this.m_mapLogic.As();
            mapLogic?.Map.Clear();
        }

        public void Set(Func<DrawCarpet, MapNode, bool> func, MapNode node)
        {
            MapLogic mapLogic = this.m_mapLogic.As();
            if (mapLogic != null && func.Invoke(this, node))
            {
                mapLogic.Map[node.Pos] = node;
            }
        }

        private void Render()
        {
            MapLogic mapLogic = this.m_mapLogic.As();
            if (mapLogic == null)
            {
                return;
            }

            Mesh mesh = this.meshFilter.sharedMesh;
            mesh.SetVertices(DrawUtil.ToList(mapLogic.s_vertices, this.s_vertices));
            mesh.SetTriangles(mapLogic.s_triangles, 0);
            mesh.SetUVs(0, DrawUtil.ToList(mapLogic.m_uv, this.m_uv));
            mesh.SetUVs(1, DrawUtil.ToList(mapLogic.m_uv2, this.m_uv2));
        }
    }
}
