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
        // 各图层在同一 Sorting Layer 内按固定偏移排布。
        // Ocean 和 Ground 负责铺底，其余层作为覆盖层叠上去。
        private const int BaseSortingOrder = -100;

        [EntitySystem]
        private static void Awake(this DrawCarpet self)
        {
        }

        [EntitySystem]
        private static void Destroy(this DrawCarpet self)
        {
            // DrawCarpet 持有的纹理和源材质来自资源系统，需要显式卸载。
            self.UnloadTexture(self.MainTexture);
            self.UnloadTexture(self.OverlayTexture);
            self.UnloadMaterial(self.SourceMaterial);
            if (self.RuntimeMaterial != null)
            {
                // 运行时材质是 new 出来的实例，不归 UGF 资源管理，因此单独销毁。
                global::UnityEngine.Object.Destroy(self.RuntimeMaterial);
            }

            self.MainTexture = null;
            self.OverlayTexture = null;
            self.SourceMaterial = null;
            self.RuntimeMaterial = null;
        }

        public static async UniTask InitAsync(this DrawCarpet self, int type)
        {
            // type 决定当前 DrawCarpet 是哪一层：
            // 0 = 海洋，1 = 内陆水域，2 = 绿色植被，3 = 常规地表，4 = 寒冷地表。
            self.CarType = type;

            // 每层都由一张主纹理和一张覆盖纹理组成，最终交给组合材质做混合。
            Texture2D mainTexture = await self.LoadTextureAsync(DrawCarpet.mainNames[type]);
            Texture2D overlayTexture = await self.LoadTextureAsync(DrawCarpet.overNames[type]);
            Material sourceMaterial = await self.LoadMaterialAsync("Custom_SpriteOverlay");
            if (self == null || self.IsDisposed)
            {
                // 异步加载结束时实体可能已销毁，资源必须及时回收。
                self.UnloadTexture(mainTexture);
                self.UnloadTexture(overlayTexture);
                self.UnloadMaterial(sourceMaterial);
                return;
            }

            self.MainTexture = mainTexture;
            self.OverlayTexture = overlayTexture;
            self.SourceMaterial = sourceMaterial;
            self.MeshFilter = self.View.GetComponent<MeshFilter>();
            self.MeshRenderer = self.View.GetComponent<MeshRenderer>();
            if (self.MeshFilter == null || self.MeshRenderer == null)
            {
                Log.Error($"nmap carpet init failed, missing renderer components on view: {self.View?.name}");
                return;
            }

            if (self.MeshRenderer != null)
            {
                // 所有 carpet 共用同一个 Sorting Layer，只用 order 区分前后层级。
                self.SetSortingLayerId(0);
                self.SetOrderInLayer(BaseSortingOrder + GetSortingOffset(type));

                // 这些地表网格是纯平面覆盖层，不需要实时阴影和探针。
                self.MeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                self.MeshRenderer.receiveShadows = false;
                self.MeshRenderer.lightProbeUsage = LightProbeUsage.Off;
                self.MeshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                // 运行时材质实例用于给当前 carpet 独立绑定纹理，避免污染源材质。
                self.EnsureRuntimeMaterial();
            }

            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                // MapLogic 负责把离散 MapNode 变成最终可提交的网格数据。
                mapLogic = self.AddComponent<MapLogic>();
                self.MapLogic = mapLogic;
            }

            mapLogic.Init();
            mapLogic.Clear();

            // MeshFilter 上的 sharedMesh 会被重复利用，避免每次生成都 new 一个 Mesh。
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
                // 属性块用来覆盖纹理参数，避免为每次更新都克隆材质。
                self.MatPropBlock = new MaterialPropertyBlock();
            }

            self.MeshRenderer.GetPropertyBlock(self.MatPropBlock);
            if (self.MainTexture != null)
            {
                // 主纹理通常作为底图输入。
                self.MatPropBlock.SetTexture("_MainTex", self.MainTexture);
            }

            if (self.OverlayTexture != null)
            {
                // Overlay 既喂给叠加纹理槽，也兼容旧的 cover 采样命名。
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

            // 先根据 MapNode 集合生成顶点、三角形和两套 UV，再把结果提交给 Mesh。
            self.Rebuild();
        }

        public static void Clear(this DrawCarpet self)
        {
            MapLogic mapLogic = self.MapLogic.As();
            // DrawMap 重新生成前只清节点索引，不在这里动 Mesh；
            // Mesh 会在 Render 时整体重建。
            mapLogic?.Map.Clear();
        }

        public static void Set(this DrawCarpet self, Func<DrawCarpet, MapNode, bool> func, MapNode node)
        {
            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic != null && func.Invoke(self, node))
            {
                // 当前 carpet 只收集属于自己图层的节点。
                mapLogic.Map[node.Pos] = node;
            }
        }

        public static void Rebuild(this DrawCarpet self)
        {
            MapLogic mapLogic = self.MapLogic.As();
            if (mapLogic == null)
            {
                return;
            }

            mapLogic.Clear();
            mapLogic.CreateMap();
            self.Render();
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

        private static int GetSortingOffset(int type)
        {
            return type switch
            {
                0 => 0,
                3 => 1,
                1 => 2,
                2 => 3,
                4 => 4,
                _ => type
            };
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

            // 这里把 MapLogic 累积出的原始缓冲一次性写回 Mesh。
            // UV0 对应主纹理图集，UV1 对应覆盖/遮罩图集。
            mesh.Clear();
            mesh.SetVertices(DrawUtil.ToList(mapLogic.Vertices, self.Vertices));
            mesh.SetTriangles(mapLogic.Triangles, 0);
            mesh.SetUVs(0, DrawUtil.ToList(mapLogic.UV, self.UV));
            mesh.SetUVs(1, DrawUtil.ToList(mapLogic.UV2, self.UV2));

            // 几何是规则平面，但不同节点拼成的整体范围会变，因此每次重算包围盒。
            // 法线也统一重算，保证材质在需要时有正确法线输入。
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }

        private static async UniTask<Texture2D> LoadTextureAsync(this DrawCarpet self, string textureName)
        {
            return await UGFComponent.Instance.LoadAssetAsync<Texture2D>(AssetUtility.GetNMapTextureAsset(textureName));
        }

        private static async UniTask<Material> LoadMaterialAsync(this DrawCarpet self, string materialName)
        {
            return await UGFComponent.Instance.LoadAssetAsync<Material>(AssetUtility.GetNMapMaterialAsset(materialName));
        }

        private static void UnloadTexture(this DrawCarpet self, Texture2D texture)
        {
            if (texture != null)
            {
                UGFComponent.Instance.UnloadAsset(texture);
            }
        }

        private static void UnloadMaterial(this DrawCarpet self, Material material)
        {
            if (material != null)
            {
                UGFComponent.Instance.UnloadAsset(material);
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
                Material sourceMaterial = self.SourceMaterial;
                if (sourceMaterial != null)
                {
                    // 优先基于资源材质克隆，保留已有 Shader 和默认参数。
                    runtimeMaterial = new Material(sourceMaterial)
                    {
                        name = $"{sourceMaterial.name}_{self.View.name}_Runtime",
                        enableInstancing = true,
                        hideFlags = HideFlags.HideAndDontSave
                    };
                }
                self.RuntimeMaterial = runtimeMaterial;
            }

            // sharedMaterial 指向当前 carpet 的运行时材质实例。
            self.MeshRenderer.sharedMaterial = runtimeMaterial;
        }
    }
}
