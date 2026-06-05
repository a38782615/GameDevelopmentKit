using System.Collections.Generic;
using Game;

using TMPro;

using UnityEngine;
using UnityEngine.Rendering;

namespace ET.Client
{
    [Code]
    public sealed class SkillHudManager : Singleton<SkillHudManager>, ISingletonAwake
    {
        [EnableClass]
        private sealed class UnitHudState
        {
            public GameObject Owner;
            public UnitType UnitType;
            public float CurrentHealth;
            public float MaxHealth;
            public float HeadOffset;
            public float HealthBarVisibleUntil;
            public float LastRenderStateLogTime;
        }

        [EnableClass]
        private sealed class FloatingTextState
        {
            public string Text;
            public Vector3 WorldPosition;
            public Color Color;
            public Mesh Mesh;
            public Material Material;
            public float WorldScale;
            public float FontSize;
            public float Duration;
            public float Elapsed;
            public float HorizontalDrift;
            public float VerticalRise;
        }

        private const float DefaultBarHeight = 0.12f;
        private const float DefaultBarYOffset = 0.28f;
        private const float DefaultHeadOffset = 1.4f;
        private const float HealthBarVisibleDuration = 3f;
        private const float PlayerBarWidth = 1.35f;
        private const float MonsterBarWidth = 1.15f;
        private const float MinForegroundWidth = 0.02f;
        private const float DefaultTextDuration = 1.2f;
        private const float TextPopScale = 0.12f;
        private const float TextBaseRise = 0.85f;
        private const bool DebugPreviewEnabled = false;
        private const bool VerboseHudLog = false;
        private const int BloodBarSubMeshCount = 3;
        private const int BloodBarBackgroundSubMesh = 0;
        private const int BloodBarPlayerSubMesh = 1;
        private const int BloodBarMonsterSubMesh = 2;

        [StaticField]
        private static readonly int HudColorId = Shader.PropertyToID("_Color");
        [StaticField]
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        [StaticField]
        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
        [StaticField]
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        [StaticField]
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        [StaticField]
        private static readonly int BlendId = Shader.PropertyToID("_Blend");
        [StaticField]
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        [StaticField]
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        [StaticField]
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        [StaticField]
        private static readonly int CullId = Shader.PropertyToID("_Cull");

        private readonly Dictionary<long, UnitHudState> unitStates = new Dictionary<long, UnitHudState>();
        private readonly Dictionary<int, FloatingTextState> floatingTextStates = new Dictionary<int, FloatingTextState>();
        private readonly List<int> pendingFloatingTextRemovals = new List<int>();
        private readonly List<Vector3> bloodBarVertices = new List<Vector3>(256);
        private readonly List<Vector2> bloodBarUvs = new List<Vector2>(256);
        private readonly List<int> bloodBarBackgroundTriangles = new List<int>(384);
        private readonly List<int> bloodBarPlayerTriangles = new List<int>(384);
        private readonly List<int> bloodBarMonsterTriangles = new List<int>(384);

        private readonly Color playerBarColor = new Color(0.20f, 0.83f, 0.45f, 0.95f);
        private readonly Color monsterBarColor = new Color(0.90f, 0.28f, 0.20f, 0.95f);
        private readonly Color barBackgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.82f);

        private MaterialPropertyBlock textPropertyBlock;
        private SkillHudRenderDriver driver;
        private SkillHudTextAtlas textAtlas;
        private TextMeshPro textGenerator;
        private Mesh quadMesh;
        private Mesh bloodBarMesh;
        private Material barMaterial;
        private Material[] bloodBarMaterials;
        private GameObject bloodBarBatchObject;
        private MeshFilter bloodBarBatchFilter;
        private MeshRenderer bloodBarBatchRenderer;
        private GameObject debugPreviewObject;
        private Transform debugPreviewTransform;
        private MeshRenderer debugPreviewRenderer;
        private Camera cachedCamera;
        private int nextFloatingTextId = 1;
        private float lastCameraStateLogTime;

        public void Awake()
        {
            EnsureRuntimeObjects();
        }

        public static SkillHudManager GetOrCreate()
        {
            return Instance ?? World.Instance.AddSingleton<SkillHudManager>();
        }

        public void RegisterUnit(long ascInstanceId, GameObject owner, UnitType unitType, float currentHealth, float maxHealth)
        {
            if (ascInstanceId == 0 || owner == null)
            {
                return;
            }

            long key = ascInstanceId;
            if (!unitStates.TryGetValue(key, out UnitHudState state))
            {
                state = new UnitHudState();
                unitStates.Add(key, state);
            }

            state.Owner = owner;
            state.UnitType = unitType;
            state.CurrentHealth = currentHealth;
            state.MaxHealth = maxHealth;
            state.HeadOffset = GetPositionFromObject(owner, "head");
            state.HealthBarVisibleUntil = 0f;
            SkillDiagFileLogger.Log($"[HUD] Register asc={ascInstanceId} unitType={unitType} owner={owner.name} hp={currentHealth:F3}/{maxHealth:F3} headOffset={state.HeadOffset:F3}");
        }

        public void UnregisterUnit(AbilitySystemComponent asc)
        {
            if (asc == null)
            {
                return;
            }

            unitStates.Remove(asc.InstanceId);
        }

        public void ClearSceneHud()
        {
            ClearBloodBarBatch();

            foreach (FloatingTextState state in floatingTextStates.Values)
            {
                ReleaseFloatingTextState(state);
            }

            floatingTextStates.Clear();
            pendingFloatingTextRemovals.Clear();
            unitStates.Clear();
        }

        public void UpdateUnitHealth(long ascInstanceId, GameObject owner, float currentHealth, float maxHealth)
        {
            if (ascInstanceId == 0 || owner == null)
            {
                return;
            }

            long key = ascInstanceId;
            if (!unitStates.TryGetValue(key, out UnitHudState state))
            {
                RegisterUnit(ascInstanceId, owner, UnitType.Monster, currentHealth, maxHealth);
                if (!unitStates.TryGetValue(key, out state))
                {
                    return;
                }
            }

            bool healthChanged = !Mathf.Approximately(state.CurrentHealth, currentHealth);
            state.Owner = owner;
            state.CurrentHealth = currentHealth;
            state.MaxHealth = maxHealth;
            state.HeadOffset = GetPositionFromObject(owner, "head");
            SkillDiagFileLogger.Log($"[HUD] Update asc={ascInstanceId} owner={owner.name} hp={currentHealth:F3}/{maxHealth:F3} changed={healthChanged} visibleUntil={state.HealthBarVisibleUntil:F3} now={Time.unscaledTime:F3}");
            if (healthChanged)
            {
                state.HealthBarVisibleUntil = Time.unscaledTime + HealthBarVisibleDuration;
                SkillDiagFileLogger.Log($"[HUD] VisibleWindow asc={ascInstanceId} until={state.HealthBarVisibleUntil:F3}");
            }
        }

        public int AddFloatingText(
            string text,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float duration,
            FloatingTextType textType)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            EnsureRuntimeObjects();
            if (textAtlas == null || !textAtlas.EnsureReady())
            {
                return 0;
            }

            textAtlas.EnsureCharacters(text);

            int handle = nextFloatingTextId++;
            float resolvedFontSize = fontSize > 0f ? fontSize : 42f;
            if (!TryBuildFloatingTextMesh(text, resolvedFontSize, out Mesh mesh, out Material material, out float worldScale))
            {
                return 0;
            }

            floatingTextStates[handle] = new FloatingTextState
            {
                Text = text,
                WorldPosition = worldPosition,
                Color = ResolveTextColor(textType, color),
                Mesh = mesh,
                Material = material,
                WorldScale = worldScale,
                FontSize = resolvedFontSize,
                Duration = duration > 0f ? duration : DefaultTextDuration,
                Elapsed = 0f,
                HorizontalDrift = UnityEngine.Random.Range(-0.18f, 0.18f),
                VerticalRise = TextBaseRise + UnityEngine.Random.Range(0.10f, 0.30f)
            };
            return handle;
        }

        public void RemoveFloatingText(int handle)
        {
            if (handle <= 0)
            {
                return;
            }

            if (!floatingTextStates.TryGetValue(handle, out FloatingTextState state))
            {
                return;
            }

            floatingTextStates.Remove(handle);
            ReleaseFloatingTextState(state);
        }

        public void Tick(float deltaTime)
        {
            if (!EnsureRuntimeObjects())
            {
                return;
            }

            Camera camera = ResolveCamera();
            if (camera == null)
            {
                SkillDiagFileLogger.Log("[HUD] Tick skip camera=null");
                return;
            }

            pendingFloatingTextRemovals.Clear();

            CollectBloodBars(camera);
            CollectFloatingTexts(deltaTime, camera);
            if (DebugPreviewEnabled)
            {
                UpdateDebugPreview(camera);
            }
        }

        protected override void Destroy()
        {
            foreach (FloatingTextState state in floatingTextStates.Values)
            {
                ReleaseFloatingTextState(state);
            }

            DestroyBloodBarBatchObject();
            unitStates.Clear();
            floatingTextStates.Clear();
            pendingFloatingTextRemovals.Clear();

            if (driver != null)
            {
                global::UnityEngine.Object.Destroy(driver.gameObject);
                driver = null;
            }

            if (barMaterial != null)
            {
                global::UnityEngine.Object.Destroy(barMaterial);
                barMaterial = null;
            }

            DestroyMaterialArray(bloodBarMaterials);
            bloodBarMaterials = null;

            if (bloodBarMesh != null)
            {
                global::UnityEngine.Object.Destroy(bloodBarMesh);
                bloodBarMesh = null;
            }

            if (quadMesh != null)
            {
                global::UnityEngine.Object.Destroy(quadMesh);
                quadMesh = null;
            }

            if (debugPreviewObject != null)
            {
                DestroyRendererMaterial(debugPreviewRenderer);
                global::UnityEngine.Object.Destroy(debugPreviewObject);
                debugPreviewObject = null;
                debugPreviewTransform = null;
                debugPreviewRenderer = null;
            }
        }

        private bool EnsureRuntimeObjects()
        {
            if (textPropertyBlock == null)
            {
                textPropertyBlock = new MaterialPropertyBlock();
            }

            if (DebugPreviewEnabled && quadMesh == null)
            {
                quadMesh = BuildQuadMesh();
            }

            if (bloodBarMesh == null)
            {
                bloodBarMesh = new Mesh
                {
                    name = "SkillHudBloodBarBatchMesh",
                    indexFormat = IndexFormat.UInt32
                };
                bloodBarMesh.MarkDynamic();
            }

            if (textAtlas == null)
            {
                textAtlas = new SkillHudTextAtlas();
            }

            if (driver == null)
            {
                GameObject driverObject = new GameObject("SkillHudRenderDriver");
                driverObject.hideFlags = HideFlags.None;
                global::UnityEngine.Object.DontDestroyOnLoad(driverObject);
                driver = driverObject.AddComponent<SkillHudRenderDriver>();
            }

            Shader barShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (barShader == null)
            {
                barShader = Shader.Find("Sprites/Default");
            }

            if (barShader == null)
            {
                barShader = Shader.Find("Game/HUD/TextBillboard");
            }

            if (barShader == null)
            {
                SkillDiagFileLogger.Log("[HUD] EnsureRuntimeObjects skip shader=null");
                return false;
            }

            if (barMaterial == null)
            {
                barMaterial = new Material(barShader)
                {
                    enableInstancing = true,
                    hideFlags = HideFlags.None
                };
                ConfigureHudMaterial(barMaterial, Color.white);
                SkillDiagFileLogger.Log($"[HUD] BaseMaterial shader={barMaterial.shader?.name} queue={barMaterial.renderQueue}");
            }

            if (bloodBarMaterials == null)
            {
                bloodBarMaterials = new[]
                {
                    CreateRuntimeBarMaterial("SkillHudBloodBar_Background_Material", barBackgroundColor),
                    CreateRuntimeBarMaterial("SkillHudBloodBar_Player_Material", playerBarColor),
                    CreateRuntimeBarMaterial("SkillHudBloodBar_Monster_Material", monsterBarColor)
                };
            }

            EnsureBloodBarBatchObject();

            if (textAtlas.EnsureReady())
            {
                EnsureTextGenerator();
            }

            if (DebugPreviewEnabled)
            {
                EnsureDebugPreviewObject();
            }

            return barMaterial != null;
        }

        private void CollectBloodBars(Camera camera)
        {
            List<long> removals = null;
            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;
            int hudLayer = ResolveHudLayer(camera);
            int drawnCount = 0;
            ClearBloodBarGeometry();

            foreach (KeyValuePair<long, UnitHudState> pair in unitStates)
            {
                UnitHudState state = pair.Value;
                if (state == null || state.Owner == null || !state.Owner.scene.IsValid() || !state.Owner.scene.isLoaded)
                {
                    if (VerboseHudLog)
                    {
                        SkillDiagFileLogger.Log($"[HUD] Skip asc={pair.Key} reason=owner_invalid");
                    }

                    removals ??= new List<long>();
                    removals.Add(pair.Key);
                    continue;
                }

                float maxHealth = Mathf.Max(0f, state.MaxHealth);
                if (maxHealth <= 0.01f)
                {
                    if (VerboseHudLog)
                    {
                        SkillDiagFileLogger.Log($"[HUD] Skip asc={pair.Key} reason=maxhp_zero hp={state.CurrentHealth:F3} max={state.MaxHealth:F3}");
                    }

                    continue;
                }

                if (Time.unscaledTime > state.HealthBarVisibleUntil)
                {
                    if (VerboseHudLog)
                    {
                        SkillDiagFileLogger.Log($"[HUD] Skip asc={pair.Key} reason=expired now={Time.unscaledTime:F3} until={state.HealthBarVisibleUntil:F3} hp={state.CurrentHealth:F3}/{maxHealth:F3}");
                    }

                    continue;
                }

                float ratio = Mathf.Clamp01(state.CurrentHealth / maxHealth);
                float barWidth = state.UnitType == UnitType.Player ? PlayerBarWidth : MonsterBarWidth;
                Vector3 anchor = state.Owner.transform.position + up * (state.HeadOffset + DefaultBarYOffset);
                Vector3 viewport = camera.WorldToViewportPoint(anchor);

                float foregroundWidth = Mathf.Max(barWidth * ratio, ratio > 0f ? MinForegroundWidth : 0f);
                Vector3 backgroundPosition = anchor;
                Vector3 foregroundPosition = anchor - right * ((barWidth - foregroundWidth) * 0.5f);
                AddBloodBarQuad(BloodBarBackgroundSubMesh, backgroundPosition, rotation, barWidth, DefaultBarHeight);

                if (foregroundWidth > 0f)
                {
                    AddBloodBarQuad(
                        state.UnitType == UnitType.Player ? BloodBarPlayerSubMesh : BloodBarMonsterSubMesh,
                        foregroundPosition,
                        rotation,
                        foregroundWidth,
                        DefaultBarHeight);
                }

                if (VerboseHudLog)
                {
                    SkillDiagFileLogger.Log(
                        $"[HUD] DrawUnit asc={pair.Key} owner={state.Owner.name} hp={state.CurrentHealth:F3}/{maxHealth:F3} ratio={ratio:F3} " +
                        $"anchor=({anchor.x:F3},{anchor.y:F3},{anchor.z:F3}) viewport=({viewport.x:F3},{viewport.y:F3},{viewport.z:F3}) " +
                        $"headOffset={state.HeadOffset:F3} barWidth={barWidth:F3}");
                }

                LogRenderStateIfNeeded(pair.Key, state);
                drawnCount++;
            }

            ApplyBloodBarBatch(hudLayer, drawnCount);

            if (drawnCount > 0)
            {
                if (VerboseHudLog)
                {
                    SkillDiagFileLogger.Log($"[HUD] Draw ascCount={drawnCount} camera={camera.name}");
                }
            }

            if (removals == null)
            {
                return;
            }

            foreach (long key in removals)
            {
                unitStates.Remove(key);
            }
        }

        private void CollectFloatingTexts(float deltaTime, Camera camera)
        {
            if (textAtlas == null || !textAtlas.EnsureReady())
            {
                return;
            }

            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;

            foreach (KeyValuePair<int, FloatingTextState> pair in floatingTextStates)
            {
                FloatingTextState state = pair.Value;
                if (state == null)
                {
                    pendingFloatingTextRemovals.Add(pair.Key);
                    continue;
                }

                state.Elapsed += deltaTime;
                float duration = Mathf.Max(0.01f, state.Duration);
                float progress = Mathf.Clamp01(state.Elapsed / duration);
                if (progress >= 1f)
                {
                    pendingFloatingTextRemovals.Add(pair.Key);
                    continue;
                }

                float scale = 1f + TextPopScale * (1f - progress);
                Color color = state.Color;
                color.a *= 1f - progress;

                Vector3 animatedPosition = state.WorldPosition;
                animatedPosition += right * (state.HorizontalDrift * progress);
                animatedPosition += up * (state.VerticalRise * progress);

                DrawFloatingText(state, animatedPosition, color, scale, rotation, camera);
            }

            foreach (int handle in pendingFloatingTextRemovals)
            {
                if (!floatingTextStates.TryGetValue(handle, out FloatingTextState state))
                {
                    continue;
                }

                floatingTextStates.Remove(handle);
                ReleaseFloatingTextState(state);
            }
        }

        private void DrawFloatingText(
            FloatingTextState state,
            Vector3 worldPosition,
            Color color,
            float scale,
            Quaternion rotation,
            Camera camera)
        {
            if (state?.Mesh == null || state.Material == null || camera == null)
            {
                return;
            }

            textPropertyBlock.Clear();
            textPropertyBlock.SetColor(FaceColorId, color);

            Graphics.DrawMesh(
                state.Mesh,
                Matrix4x4.TRS(worldPosition, rotation, Vector3.one * (state.WorldScale * scale)),
                state.Material,
                0,
                camera,
                0,
                textPropertyBlock,
                ShadowCastingMode.Off,
                false,
                null,
                LightProbeUsage.Off,
                null);
        }

        private void EnsureBloodBarBatchObject()
        {
            if (bloodBarBatchObject != null || driver == null || bloodBarMesh == null || bloodBarMaterials == null)
            {
                return;
            }

            bloodBarBatchObject = new GameObject("SkillHudBloodBarBatch");
            bloodBarBatchObject.hideFlags = HideFlags.None;
            bloodBarBatchObject.transform.SetParent(driver.transform, false);
            bloodBarBatchObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            bloodBarBatchObject.transform.localScale = Vector3.one;

            bloodBarBatchFilter = bloodBarBatchObject.AddComponent<MeshFilter>();
            bloodBarBatchFilter.sharedMesh = bloodBarMesh;

            bloodBarBatchRenderer = bloodBarBatchObject.AddComponent<MeshRenderer>();
            bloodBarBatchRenderer.sharedMaterials = bloodBarMaterials;
            bloodBarBatchRenderer.shadowCastingMode = ShadowCastingMode.Off;
            bloodBarBatchRenderer.receiveShadows = false;
            bloodBarBatchRenderer.lightProbeUsage = LightProbeUsage.Off;
            bloodBarBatchRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            bloodBarBatchRenderer.allowOcclusionWhenDynamic = false;
            bloodBarBatchRenderer.forceRenderingOff = false;
            bloodBarBatchRenderer.rendererPriority = 100;
            bloodBarBatchRenderer.sortingOrder = 5000;
            bloodBarBatchRenderer.enabled = false;
            SkillDiagFileLogger.Log($"[HUD] CreateBloodBarBatchObject materials={bloodBarMaterials.Length} shader={bloodBarMaterials[0]?.shader?.name}");
        }

        private void ClearBloodBarGeometry()
        {
            bloodBarVertices.Clear();
            bloodBarUvs.Clear();
            bloodBarBackgroundTriangles.Clear();
            bloodBarPlayerTriangles.Clear();
            bloodBarMonsterTriangles.Clear();
        }

        private void AddBloodBarQuad(int subMeshIndex, Vector3 center, Quaternion rotation, float width, float height)
        {
            Vector3 right = rotation * Vector3.right * (width * 0.5f);
            Vector3 up = rotation * Vector3.up * (height * 0.5f);
            int vertexStart = bloodBarVertices.Count;

            bloodBarVertices.Add(center - right - up);
            bloodBarVertices.Add(center - right + up);
            bloodBarVertices.Add(center + right + up);
            bloodBarVertices.Add(center + right - up);

            bloodBarUvs.Add(new Vector2(0f, 0f));
            bloodBarUvs.Add(new Vector2(0f, 1f));
            bloodBarUvs.Add(new Vector2(1f, 1f));
            bloodBarUvs.Add(new Vector2(1f, 0f));

            List<int> triangles = GetBloodBarTriangles(subMeshIndex);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 1);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart);
            triangles.Add(vertexStart + 2);
            triangles.Add(vertexStart + 3);
        }

        private List<int> GetBloodBarTriangles(int subMeshIndex)
        {
            switch (subMeshIndex)
            {
                case BloodBarPlayerSubMesh:
                    return bloodBarPlayerTriangles;
                case BloodBarMonsterSubMesh:
                    return bloodBarMonsterTriangles;
                default:
                    return bloodBarBackgroundTriangles;
            }
        }

        private void ApplyBloodBarBatch(int layer, int drawnCount)
        {
            if (bloodBarMesh == null || bloodBarBatchObject == null || bloodBarBatchRenderer == null)
            {
                return;
            }

            if (drawnCount <= 0 || bloodBarVertices.Count == 0)
            {
                ClearBloodBarBatch();
                return;
            }

            bloodBarBatchObject.layer = layer;
            bloodBarBatchObject.SetActive(true);
            bloodBarBatchRenderer.enabled = true;

            bloodBarMesh.Clear(false);
            bloodBarMesh.SetVertices(bloodBarVertices);
            bloodBarMesh.SetUVs(0, bloodBarUvs);
            bloodBarMesh.subMeshCount = BloodBarSubMeshCount;
            bloodBarMesh.SetTriangles(bloodBarBackgroundTriangles, BloodBarBackgroundSubMesh, false);
            bloodBarMesh.SetTriangles(bloodBarPlayerTriangles, BloodBarPlayerSubMesh, false);
            bloodBarMesh.SetTriangles(bloodBarMonsterTriangles, BloodBarMonsterSubMesh, false);
            bloodBarMesh.RecalculateBounds();
        }

        private Material CreateRuntimeBarMaterial(string name, Color color)
        {
            Material material = new Material(barMaterial)
            {
                name = name,
                hideFlags = HideFlags.None,
                renderQueue = (int)RenderQueue.Transparent + 100
            };
            ConfigureHudMaterial(material, color);
            return material;
        }

        private static void ConfigureHudMaterial(Material material, Color color)
        {
            if (material == null)
            {
                return;
            }

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent + 100;

            if (material.HasProperty(MainTexId))
            {
                material.SetTexture(MainTexId, Texture2D.whiteTexture);
            }

            if (material.HasProperty(HudColorId))
            {
                material.SetColor(HudColorId, color);
            }

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(SurfaceId))
            {
                material.SetFloat(SurfaceId, 1f);
            }

            if (material.HasProperty(BlendId))
            {
                material.SetFloat(BlendId, 0f);
            }

            if (material.HasProperty(SrcBlendId))
            {
                material.SetFloat(SrcBlendId, (float)BlendMode.SrcAlpha);
            }

            if (material.HasProperty(DstBlendId))
            {
                material.SetFloat(DstBlendId, (float)BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty(ZWriteId))
            {
                material.SetFloat(ZWriteId, 0f);
            }

            if (material.HasProperty(CullId))
            {
                material.SetFloat(CullId, (float)CullMode.Off);
            }
        }

        private void EnsureDebugPreviewObject()
        {
            if (debugPreviewObject != null || driver == null || barMaterial == null || quadMesh == null)
            {
                return;
            }

            debugPreviewObject = new GameObject("SkillHudDebugPreview");
            debugPreviewObject.transform.SetParent(driver.transform, false);
            debugPreviewTransform = debugPreviewObject.transform;

            MeshFilter meshFilter = debugPreviewObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = quadMesh;

            debugPreviewRenderer = debugPreviewObject.AddComponent<MeshRenderer>();
            debugPreviewRenderer.sharedMaterial = CreateRuntimeBarMaterial("SkillHudDebugPreview_Material", new Color(1f, 0.95f, 0.1f, 1f));
            debugPreviewRenderer.shadowCastingMode = ShadowCastingMode.Off;
            debugPreviewRenderer.receiveShadows = false;
            debugPreviewRenderer.lightProbeUsage = LightProbeUsage.Off;
            debugPreviewRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            debugPreviewRenderer.allowOcclusionWhenDynamic = false;
            debugPreviewRenderer.forceRenderingOff = false;
            debugPreviewRenderer.rendererPriority = 100;
            debugPreviewRenderer.sortingOrder = 6000;
            SkillDiagFileLogger.Log($"[HUD] CreateDebugPreviewObject material={debugPreviewRenderer.sharedMaterial?.name} shader={debugPreviewRenderer.sharedMaterial?.shader?.name}");
        }

        private void UpdateDebugPreview(Camera camera)
        {
            if (debugPreviewObject == null || debugPreviewTransform == null || debugPreviewRenderer == null || camera == null)
            {
                return;
            }

            debugPreviewObject.SetActive(true);
            debugPreviewObject.layer = ResolveHudLayer(camera);
            debugPreviewTransform.SetPositionAndRotation(camera.transform.position + camera.transform.forward * 2f, camera.transform.rotation);
            debugPreviewTransform.localScale = new Vector3(2f, 0.3f, 1f);

            SetRendererColor(debugPreviewRenderer, new Color(1f, 0.95f, 0.1f, 1f));
            LogCameraStateIfNeeded(camera);
        }

        private void SetRendererColor(MeshRenderer meshRenderer, Color color)
        {
            if (meshRenderer == null)
            {
                return;
            }

            Material material = meshRenderer.sharedMaterial;
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(HudColorId))
            {
                material.SetColor(HudColorId, color);
            }

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }
        }

        private static int ResolveHudLayer(Camera camera)
        {
            if (camera == null)
            {
                return 0;
            }

            int cullingMask = camera.cullingMask;
            if ((cullingMask & 1) != 0)
            {
                return 0;
            }

            for (int layer = 1; layer < 32; ++layer)
            {
                if ((cullingMask & (1 << layer)) != 0)
                {
                    return layer;
                }
            }

            return 0;
        }

        private void LogCameraStateIfNeeded(Camera camera)
        {
            if (camera == null || Time.unscaledTime < lastCameraStateLogTime + 1f)
            {
                return;
            }

            lastCameraStateLogTime = Time.unscaledTime;
            SkillDiagFileLogger.Log(
                $"[HUD] CameraState camera={camera.name} cullingMask={camera.cullingMask} hudLayer={ResolveHudLayer(camera)} " +
                $"debugActive={debugPreviewObject?.activeSelf ?? false} debugVisible={debugPreviewRenderer?.isVisible ?? false} " +
                $"debugLayer={debugPreviewObject?.layer ?? -1} debugEnabled={debugPreviewRenderer?.enabled ?? false}");
        }

        private void LogRenderStateIfNeeded(long ascInstanceId, UnitHudState state)
        {
            if (!VerboseHudLog || state == null || Time.unscaledTime < state.LastRenderStateLogTime + 0.5f)
            {
                return;
            }

            state.LastRenderStateLogTime = Time.unscaledTime;
            LogBatchRenderState(ascInstanceId);
        }

        private void LogBatchRenderState(long ascInstanceId)
        {
            if (bloodBarBatchObject == null || bloodBarBatchRenderer == null || bloodBarMesh == null)
            {
                SkillDiagFileLogger.Log($"[HUD] BatchRenderState asc={ascInstanceId} batch=null");
                return;
            }

            Bounds bounds = bloodBarBatchRenderer.bounds;
            SkillDiagFileLogger.Log(
                $"[HUD] BatchRenderState asc={ascInstanceId} active={bloodBarBatchObject.activeSelf} layer={bloodBarBatchObject.layer} enabled={bloodBarBatchRenderer.enabled} visible={bloodBarBatchRenderer.isVisible} " +
                $"vertexCount={bloodBarMesh.vertexCount} subMeshes={bloodBarMesh.subMeshCount} " +
                $"bounds=({bounds.center.x:F3},{bounds.center.y:F3},{bounds.center.z:F3};{bounds.size.x:F3},{bounds.size.y:F3},{bounds.size.z:F3})");
        }

        private static void DestroyRendererMaterial(MeshRenderer meshRenderer)
        {
            if (meshRenderer == null)
            {
                return;
            }

            Material material = meshRenderer.sharedMaterial;
            if (material != null)
            {
                global::UnityEngine.Object.Destroy(material);
            }
        }

        private void ClearBloodBarBatch()
        {
            ClearBloodBarGeometry();

            if (bloodBarMesh != null)
            {
                bloodBarMesh.Clear(false);
            }

            if (bloodBarBatchRenderer != null)
            {
                bloodBarBatchRenderer.enabled = false;
            }
        }

        private void DestroyBloodBarBatchObject()
        {
            ClearBloodBarBatch();

            if (bloodBarBatchObject != null)
            {
                global::UnityEngine.Object.Destroy(bloodBarBatchObject);
                bloodBarBatchObject = null;
                bloodBarBatchFilter = null;
                bloodBarBatchRenderer = null;
            }
        }

        private static void DestroyMaterialArray(Material[] materials)
        {
            if (materials == null)
            {
                return;
            }

            foreach (Material material in materials)
            {
                if (material != null)
                {
                    global::UnityEngine.Object.Destroy(material);
                }
            }
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            cachedCamera = GameEntry.Camera?.CurrentSceneCamera;
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            {
                cachedCamera = Camera.main;
            }

            SkillDiagFileLogger.Log($"[HUD] ResolveCamera result={(cachedCamera == null ? "null" : cachedCamera.name)}");

            return cachedCamera;
        }

        private static Mesh BuildQuadMesh()
        {
            GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.hideFlags = HideFlags.HideAndDontSave;

            MeshFilter meshFilter = quadObject.GetComponent<MeshFilter>();
            Mesh sourceMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            Mesh mesh = sourceMesh != null ? global::UnityEngine.Object.Instantiate(sourceMesh) : null;

            if (Application.isPlaying)
            {
                global::UnityEngine.Object.Destroy(quadObject);
            }
            else
            {
                global::UnityEngine.Object.DestroyImmediate(quadObject);
            }

            if (mesh == null)
            {
                return null;
            }

            mesh.name = "SkillHudQuad";
            return mesh;
        }

        private void EnsureTextGenerator()
        {
            if (textGenerator != null || driver == null || textAtlas == null || !textAtlas.EnsureReady())
            {
                return;
            }

            GameObject generatorObject = new GameObject("SkillHudTextGenerator");
            generatorObject.hideFlags = HideFlags.HideAndDontSave;
            generatorObject.transform.SetParent(driver.transform, false);

            textGenerator = generatorObject.AddComponent<TextMeshPro>();
            textGenerator.font = textAtlas.FontAsset;
            textGenerator.fontSharedMaterial = textAtlas.FontAsset.material;
            textGenerator.alignment = TextAlignmentOptions.Center;
            textGenerator.enableWordWrapping = false;
            textGenerator.overflowMode = TextOverflowModes.Overflow;
            textGenerator.richText = false;
            textGenerator.text = string.Empty;

            MeshRenderer meshRenderer = textGenerator.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                meshRenderer.lightProbeUsage = LightProbeUsage.Off;
                meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            }
        }

        private bool TryBuildFloatingTextMesh(string text, float fontSize, out Mesh mesh, out Material material, out float worldScale)
        {
            mesh = null;
            material = null;
            worldScale = 0f;
            if (textGenerator == null || textAtlas == null || !textAtlas.EnsureReady() || string.IsNullOrEmpty(text))
            {
                return false;
            }

            textGenerator.font = textAtlas.FontAsset;
            textGenerator.fontSharedMaterial = textAtlas.FontAsset.material;
            textGenerator.text = text;
            textGenerator.fontSize = fontSize;
            textGenerator.color = Color.white;
            textGenerator.ForceMeshUpdate(true, true);

            Mesh sourceMesh = textGenerator.mesh;
            if (sourceMesh == null || sourceMesh.vertexCount <= 0)
            {
                return false;
            }

            mesh = global::UnityEngine.Object.Instantiate(sourceMesh);
            mesh.name = "SkillHudFloatingTextMesh";
            mesh.RecalculateBounds();

            Bounds bounds = mesh.bounds;
            float meshHeight = Mathf.Max(bounds.size.y, 0.0001f);
            float targetHeight = Mathf.Max(textAtlas.GetLineHeight(fontSize), 0.01f);
            worldScale = targetHeight / meshHeight;
            material = textGenerator.fontSharedMaterial;
            return material != null;
        }

        private static void ReleaseFloatingTextState(FloatingTextState state)
        {
            if (state?.Mesh != null)
            {
                global::UnityEngine.Object.Destroy(state.Mesh);
                state.Mesh = null;
            }

            state.Material = null;
        }

        private static float GetPositionFromObject(GameObject obj, string bindingName)
        {
            if (obj == null) return DefaultHeadOffset;

            if (string.IsNullOrEmpty(bindingName))
                return DefaultHeadOffset;

            Transform bindingPoint = obj.transform.Find(bindingName);
            if (bindingPoint != null)
            {
                float offset = Mathf.Max(bindingPoint.position.y - obj.transform.position.y, 0f);
                SkillDiagFileLogger.Log($"[HUD] HeadOffset binding={bindingName} obj={obj.name} offset={offset:F3}");
                return offset;
            }

            bindingPoint = FindChildRecursive(obj.transform, bindingName);
            if (bindingPoint != null)
            {
                float offset = Mathf.Max(bindingPoint.position.y - obj.transform.position.y, 0f);
                SkillDiagFileLogger.Log($"[HUD] HeadOffsetRecursive binding={bindingName} obj={obj.name} offset={offset:F3}");
                return offset;
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            if (renderers != null && renderers.Length > 0)
            {
                Bounds bounds = renderers[0].bounds;
                for (int index = 1; index < renderers.Length; ++index)
                {
                    bounds.Encapsulate(renderers[index].bounds);
                }

                float offset = Mathf.Max(bounds.max.y - obj.transform.position.y, DefaultHeadOffset);
                SkillDiagFileLogger.Log($"[HUD] HeadOffsetBounds obj={obj.name} offset={offset:F3}");
                return offset;
            }

            SkillDiagFileLogger.Log($"[HUD] HeadOffsetDefault obj={obj.name} offset={DefaultHeadOffset:F3}");
            return DefaultHeadOffset;
        }
        private static Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static Color ResolveTextColor(FloatingTextType textType, Color customColor)
        {
            if (customColor != Color.white)
            {
                return customColor;
            }

            switch (textType)
            {
                case FloatingTextType.Damage:
                    return new Color(1f, 0.35f, 0.35f, 1f);
                case FloatingTextType.Heal:
                    return new Color(0.35f, 1f, 0.45f, 1f);
                case FloatingTextType.Status:
                    return new Color(1f, 0.95f, 0.35f, 1f);
                case FloatingTextType.Experience:
                    return new Color(0.35f, 0.95f, 1f, 1f);
                case FloatingTextType.Gold:
                    return new Color(1f, 0.84f, 0.20f, 1f);
                default:
                    return Color.white;
            }
        }
    }
}
