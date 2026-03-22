using System.Collections.Generic;

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

        private struct QuadInstance
        {
            public Matrix4x4 Matrix;
            public Vector4 Color;
            public Vector4 UvRect;
        }

        private const int MaxBatchSize = 1023;
        private const float DefaultBarHeight = 0.12f;
        private const float DefaultBarYOffset = 0.28f;
        private const float DefaultHeadOffset = 1.4f;
        private const float PlayerBarWidth = 1.35f;
        private const float MonsterBarWidth = 1.15f;
        private const float MinForegroundWidth = 0.02f;
        private const float DefaultTextDuration = 1.2f;
        private const float TextPopScale = 0.12f;
        private const float TextBaseRise = 0.85f;

        [StaticField]
        private static readonly int HudColorId = Shader.PropertyToID("_HudColor");
        [StaticField]
        private static readonly int HudUvRectId = Shader.PropertyToID("_HudUvRect");
        [StaticField]
        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
        [StaticField]
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        [StaticField]
        private static readonly Vector4 FullUvRect = new Vector4(0f, 0f, 1f, 1f);

        private readonly Dictionary<long, UnitHudState> unitStates = new Dictionary<long, UnitHudState>();
        private readonly Dictionary<int, FloatingTextState> floatingTextStates = new Dictionary<int, FloatingTextState>();
        private readonly List<int> pendingFloatingTextRemovals = new List<int>();
        private readonly List<QuadInstance> backgroundInstances = new List<QuadInstance>(128);
        private readonly List<QuadInstance> foregroundInstances = new List<QuadInstance>(128);

        private readonly Matrix4x4[] matrixBuffer = new Matrix4x4[MaxBatchSize];
        private readonly Vector4[] colorBuffer = new Vector4[MaxBatchSize];
        private readonly Vector4[] uvRectBuffer = new Vector4[MaxBatchSize];

        private readonly Color playerBarColor = new Color(0.20f, 0.83f, 0.45f, 0.95f);
        private readonly Color monsterBarColor = new Color(0.90f, 0.28f, 0.20f, 0.95f);
        private readonly Color barBackgroundColor = new Color(0.05f, 0.07f, 0.10f, 0.82f);

        private MaterialPropertyBlock propertyBlock;
        private MaterialPropertyBlock textPropertyBlock;
        private SkillHudRenderDriver driver;
        private SkillHudTextAtlas textAtlas;
        private TextMeshPro textGenerator;
        private Mesh quadMesh;
        private Material barMaterial;
        private Camera cachedCamera;
        private int nextFloatingTextId = 1;

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
            int unitCount = unitStates.Count;
            int floatingTextCount = floatingTextStates.Count;

            unitStates.Clear();

            foreach (FloatingTextState state in floatingTextStates.Values)
            {
                ReleaseFloatingTextState(state);
            }

            floatingTextStates.Clear();
            pendingFloatingTextRemovals.Clear();
            backgroundInstances.Clear();
            foregroundInstances.Clear();
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
                return;
            }

            state.Owner = owner;
            state.CurrentHealth = currentHealth;
            state.MaxHealth = maxHealth;
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
                return;
            }

            backgroundInstances.Clear();
            foregroundInstances.Clear();
            pendingFloatingTextRemovals.Clear();

            CollectBloodBars(camera);
            CollectFloatingTexts(deltaTime, camera);

            DrawInstances(backgroundInstances, barMaterial);
            DrawInstances(foregroundInstances, barMaterial);
        }

        protected override void Destroy()
        {
            foreach (FloatingTextState state in floatingTextStates.Values)
            {
                ReleaseFloatingTextState(state);
            }

            unitStates.Clear();
            floatingTextStates.Clear();
            pendingFloatingTextRemovals.Clear();
            backgroundInstances.Clear();
            foregroundInstances.Clear();

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

            if (quadMesh != null)
            {
                global::UnityEngine.Object.Destroy(quadMesh);
                quadMesh = null;
            }
        }

        private bool EnsureRuntimeObjects()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }

            if (textPropertyBlock == null)
            {
                textPropertyBlock = new MaterialPropertyBlock();
            }

            if (quadMesh == null)
            {
                quadMesh = BuildQuadMesh();
            }

            if (textAtlas == null)
            {
                textAtlas = new SkillHudTextAtlas();
            }

            if (driver == null)
            {
                GameObject driverObject = new GameObject("SkillHudRenderDriver");
                driverObject.hideFlags = HideFlags.HideAndDontSave;
                global::UnityEngine.Object.DontDestroyOnLoad(driverObject);
                driver = driverObject.AddComponent<SkillHudRenderDriver>();
            }

            Shader barShader = Shader.Find("Game/HUD/InstancedBillboard");
            if (barShader == null)
            {
                return false;
            }

            if (barMaterial == null)
            {
                barMaterial = new Material(barShader)
                {
                    enableInstancing = true,
                    hideFlags = HideFlags.HideAndDontSave
                };
                barMaterial.SetTexture(MainTexId, Texture2D.whiteTexture);
            }

            if (textAtlas.EnsureReady())
            {
                EnsureTextGenerator();
            }

            return barMaterial != null;
        }

        private void CollectBloodBars(Camera camera)
        {
            List<long> removals = null;
            Quaternion rotation = Quaternion.LookRotation(camera.transform.forward, camera.transform.up);
            Vector3 right = camera.transform.right;
            Vector3 up = camera.transform.up;

            foreach (KeyValuePair<long, UnitHudState> pair in unitStates)
            {
                UnitHudState state = pair.Value;
                if (state == null || state.Owner == null || !state.Owner.scene.IsValid() || !state.Owner.scene.isLoaded)
                {
                    removals ??= new List<long>();
                    removals.Add(pair.Key);
                    continue;
                }

                float maxHealth = Mathf.Max(0f, state.MaxHealth);
                if (maxHealth <= 0.01f)
                {
                    continue;
                }

                float ratio = Mathf.Clamp01(state.CurrentHealth / maxHealth);
                float barWidth = state.UnitType == UnitType.Player ? PlayerBarWidth : MonsterBarWidth;
                Vector3 anchor = state.Owner.transform.position + up * (state.HeadOffset + DefaultBarYOffset);
                Vector3 backgroundBottomLeft = anchor - right * (barWidth * 0.5f);
                AddQuad(backgroundInstances, backgroundBottomLeft, barWidth, DefaultBarHeight, rotation, barBackgroundColor, FullUvRect);

                float foregroundWidth = Mathf.Max(barWidth * ratio, ratio > 0f ? MinForegroundWidth : 0f);
                if (foregroundWidth > 0f)
                {
                    AddQuad(
                        foregroundInstances,
                        backgroundBottomLeft,
                        foregroundWidth,
                        DefaultBarHeight,
                        rotation,
                        state.UnitType == UnitType.Player ? playerBarColor : monsterBarColor,
                        FullUvRect);
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

        private void AddQuad(
            List<QuadInstance> batch,
            Vector3 bottomLeft,
            float width,
            float height,
            Quaternion rotation,
            Color color,
            Vector4 uvRect)
        {
            batch.Add(new QuadInstance
            {
                Matrix = Matrix4x4.TRS(bottomLeft, rotation, new Vector3(width, height, 1f)),
                Color = color,
                UvRect = uvRect
            });
        }

        private void DrawInstances(List<QuadInstance> instances, Material material)
        {
            if (instances.Count == 0 || material == null || quadMesh == null)
            {
                return;
            }

            for (int start = 0; start < instances.Count; start += MaxBatchSize)
            {
                int count = Mathf.Min(MaxBatchSize, instances.Count - start);
                for (int index = 0; index < count; ++index)
                {
                    QuadInstance instance = instances[start + index];
                    matrixBuffer[index] = instance.Matrix;
                    colorBuffer[index] = instance.Color;
                    uvRectBuffer[index] = instance.UvRect;
                }

                propertyBlock.Clear();
                propertyBlock.SetVectorArray(HudColorId, colorBuffer);
                propertyBlock.SetVectorArray(HudUvRectId, uvRectBuffer);
                Graphics.DrawMeshInstanced(
                    quadMesh,
                    0,
                    material,
                    matrixBuffer,
                    count,
                    propertyBlock,
                    ShadowCastingMode.Off,
                    false,
                    0,
                    cachedCamera,
                    LightProbeUsage.Off);
            }
        }

        private Camera ResolveCamera()
        {
            if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
            {
                return cachedCamera;
            }

            cachedCamera = Camera.main;
            return cachedCamera;
        }

        private static Mesh BuildQuadMesh()
        {
            Mesh mesh = new Mesh
            {
                name = "SkillHudQuad"
            };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
                new Vector3(1f, 1f, 0f)
            });
            mesh.SetUVs(0, new List<Vector2>
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 2, 3, 1 }, 0);
            mesh.RecalculateBounds();
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
                return bindingPoint.position.y;

            bindingPoint = FindChildRecursive(obj.transform, bindingName);
            if (bindingPoint != null)
                return bindingPoint.position.y;

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
