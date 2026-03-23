using System;
using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;
using UnityEngine.Rendering;

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

        [EntitySystem]
        private static void Destroy(this DrawCarpet self)
        {
            self.UnloadTexture(self.MainTexture);
            self.UnloadTexture(self.OverlayTexture);
            if (self.RuntimeMaterial != null)
            {
                global::UnityEngine.Object.Destroy(self.RuntimeMaterial);
            }

            self.MainTexture = null;
            self.OverlayTexture = null;
            self.RuntimeMaterial = null;
        }

        public static async UniTask InitAsync(this DrawCarpet self, int type)
        {
            self.CarType = type;
            Texture2D mainTexture = await self.LoadTextureAsync(DrawCarpet.mainNames[type]);
            Texture2D overlayTexture = await self.LoadTextureAsync(DrawCarpet.overNames[type]);
            if (self == null || self.IsDisposed)
            {
                self.UnloadTexture(mainTexture);
                self.UnloadTexture(overlayTexture);
                return;
            }

            self.MainTexture = mainTexture;
            self.OverlayTexture = overlayTexture;
            self.MeshFilter = self.View.GetComponent<MeshFilter>();
            self.MeshRenderer = self.View.GetComponent<MeshRenderer>();
            if (self.MeshFilter == null || self.MeshRenderer == null)
            {
                Log.Error($"nmap carpet init failed, missing renderer components on view: {self.View?.name}");
                return;
            }

            if (self.MeshRenderer != null)
            {
                self.SetSortingLayerId(0);
                self.SetOrderInLayer(type);
                self.MeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                self.MeshRenderer.receiveShadows = false;
                self.MeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                self.MeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                self.EnsureRuntimeMaterial();
            }

            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                mapLogic = self.AddComponent<MapLogic>();
                self.MapLogic = mapLogic;
            }

            mapLogic.Init();
            mapLogic.Clear();

            Mesh mesh = self.MeshFilter.sharedMesh;
            if (mesh == null)
            {
                mesh = new Mesh
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    name = $"{self.View.name}_mesh"
                };
                self.MeshFilter.sharedMesh = mesh;
            }

            mesh.indexFormat = IndexFormat.UInt32;
            mesh.MarkDynamic();
            mesh.Clear();
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
                self.MatPropBlock.SetTexture("_OverlayTex", self.OverlayTexture);
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
            if (mapLogic == null || self.MeshFilter == null)
            {
                return;
            }

            Mesh mesh = self.MeshFilter.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            mesh.Clear();
            mesh.SetVertices(DrawUtil.ToList(mapLogic.Vertices, self.Vertices));
            mesh.SetTriangles(mapLogic.Triangles, 0);
            mesh.SetUVs(0, DrawUtil.ToList(mapLogic.UV, self.UV));
            mesh.SetUVs(1, DrawUtil.ToList(mapLogic.UV2, self.UV2));
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private static async UniTask<Texture2D> LoadTextureAsync(this DrawCarpet self, string textureName)
        {
            return await UGFComponent.Instance.LoadAssetAsync<Texture2D>(AssetUtility.GetNMapTextureAsset(textureName));
        }

        private static void UnloadTexture(this DrawCarpet self, Texture2D texture)
        {
            if (texture != null)
            {
                UGFComponent.Instance.UnloadAsset(texture);
            }
        }

        private static void EnsureRuntimeMaterial(this DrawCarpet self)
        {
            if (self.MeshRenderer == null)
            {
                return;
            }

            Material runtimeMaterial = self.RuntimeMaterial;
            if (runtimeMaterial == null)
            {
                Shader shader = Shader.Find("Game/NMapURP");
                if (shader == null)
                {
                    Log.Error($"nmap carpet init failed, cannot resolve urp shader for view: {self.View?.name}");
                    return;
                }

                runtimeMaterial = new Material(shader)
                {
                    name = $"NMapURP_{self.View.name}_Runtime",
                    enableInstancing = true,
                    hideFlags = HideFlags.HideAndDontSave
                };

                self.RuntimeMaterial = runtimeMaterial;
            }

            self.MeshRenderer.sharedMaterial = runtimeMaterial;
        }
    }
}
