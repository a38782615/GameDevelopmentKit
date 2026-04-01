using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public static class WaterRenderTestSceneBuilder
    {
        private const string ScenePath = "Assets/Res/Scene/WaterRenderTest.unity";
        private const string QuadMeshPath = "Assets/Res/Mesh/nmap/WaterRenderTestQuad.asset";
        private const string FullMaskPath = "Assets/Res/Texture/nmap/WaterTest_FullMask.png";
        private const string ShoreMaskPath = "Assets/Res/Texture/nmap/WaterTest_ShoreMask.png";
        private const string OpenMaterialPath = "Assets/Res/Material/nmap/Custom_WaterFlow_Test_Open.mat";
        private const string ShoreMaterialPath = "Assets/Res/Material/nmap/Custom_WaterFlow_Test_Shore.mat";
        private const string BackdropMaterialPath = "Assets/Res/Material/nmap/WaterTest_Backdrop.mat";
        private const string MainTexturePath = "Assets/Res/Texture/nmap/Water_DarkTile.png";
        private const string OverlayTexturePath = "Assets/Res/Texture/nmap/Water_LightTile.png";
        private const string WaterShaderName = "Game/NMap/WaterFlow";
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";

        [MenuItem("Game/Tool/Create Water Render Test Scene")]
        public static void CreateScene()
        {
            EnsureFolder("Assets/Res/Mesh");
            EnsureFolder("Assets/Res/Mesh/nmap");

            Mesh quadMesh = EnsureQuadMesh();
            Texture2D fullMask = EnsureFullMaskTexture();
            Texture2D shoreMask = EnsureShoreMaskTexture();
            Material openMaterial = EnsureWaterMaterial(OpenMaterialPath, fullMask, 0.16f, 0.24f, 0.20f, 0.36f, 0.0f);
            Material shoreMaterial = EnsureWaterMaterial(ShoreMaterialPath, shoreMask, 0.16f, 0.24f, 0.20f, 0.46f, 0.28f);
            Material backdropMaterial = EnsureBackdropMaterial();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            CreateBackdrop("Backdrop_Deep", new Vector3(1.2f, 0f, 1f), new Vector2(10.4f, 5.8f), new Color(0.04f, 0.16f, 0.22f, 1f), backdropMaterial, quadMesh);
            CreateBackdrop("Backdrop_Shore", new Vector3(-4.2f, 0f, 0.8f), new Vector2(2.6f, 5.8f), new Color(0.73f, 0.64f, 0.47f, 1f), backdropMaterial, quadMesh);
            CreateConnectedWaterTiles(openMaterial, shoreMaterial, quadMesh);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.11f, 0.14f, 1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 50f;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;
        }

        private static void CreateBackdrop(string objectName, Vector3 position, Vector2 size, Color color, Material templateMaterial, Mesh quadMesh)
        {
            Material material = new Material(templateMaterial);
            material.name = objectName + "_Mat";
            material.SetColor("_BaseColor", color);
            CreateQuadObject(objectName, position, size, material, quadMesh);
        }

        private static void CreateWaterPlane(string objectName, Vector3 position, Vector2 size, Material material, Mesh quadMesh)
        {
            CreateQuadObject(objectName, position, size, material, quadMesh);
        }

        private static void CreateConnectedWaterTiles(Material openMaterial, Material shoreMaterial, Mesh quadMesh)
        {
            const int ColumnCount = 5;
            const int RowCount = 2;
            const float TileWidth = 2.1f;
            const float TileHeight = 2.4f;
            const float StartX = -4.2f;
            const float StartY = -1.25f;

            for (int row = 0; row < RowCount; row++)
            {
                for (int column = 0; column < ColumnCount; column++)
                {
                    float x = StartX + column * TileWidth;
                    float y = StartY + row * TileHeight;
                    bool isShoreColumn = column == 0;
                    Material material = isShoreColumn ? shoreMaterial : openMaterial;
                    CreateWaterPlane(
                        $"Water_{row}_{column}",
                        new Vector3(x, y, 0f),
                        new Vector2(TileWidth, TileHeight),
                        material,
                        quadMesh);
                }
            }
        }

        private static void CreateQuadObject(string objectName, Vector3 position, Vector2 size, Material material, Mesh quadMesh)
        {
            GameObject quadObject = new GameObject(objectName);
            quadObject.transform.position = position;
            quadObject.transform.localScale = new Vector3(size.x, size.y, 1f);

            MeshFilter meshFilter = quadObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = quadMesh;

            MeshRenderer meshRenderer = quadObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static Mesh EnsureQuadMesh()
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(QuadMeshPath);
            if (mesh != null)
            {
                return mesh;
            }

            mesh = new Mesh
            {
                name = "WaterRenderTestQuad"
            };

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 3 };
            Vector2[] uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.uv = uv;
            mesh.uv2 = uv;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();

            AssetDatabase.CreateAsset(mesh, QuadMeshPath);
            return mesh;
        }

        private static Texture2D EnsureFullMaskTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(FullMaskPath);
            if (texture != null)
            {
                return texture;
            }

            return CreateTextureAsset(
                FullMaskPath,
                256,
                256,
                (x, y) => Color.white,
                TextureWrapMode.Clamp);
        }

        private static Texture2D EnsureShoreMaskTexture()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ShoreMaskPath);
            if (texture != null)
            {
                return texture;
            }

            return CreateTextureAsset(
                ShoreMaskPath,
                256,
                256,
                (x, y) =>
                {
                    float xf = x / 255f;
                    float yf = y / 255f;
                    float wave = Mathf.Sin(yf * Mathf.PI * 6f) * 0.03f + Mathf.Sin(yf * Mathf.PI * 16f) * 0.012f;
                    float edge = Mathf.SmoothStep(0.18f + wave, 0.78f + wave, xf);
                    return new Color(edge, edge, edge, 1f);
                },
                TextureWrapMode.Clamp);
        }

        private static Texture2D CreateTextureAsset(string path, int width, int height, System.Func<int, int, Color> colorFunc, TextureWrapMode wrapMode)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false)
            {
                wrapMode = wrapMode,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[x + y * width] = colorFunc(x, y);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = wrapMode;
                importer.filterMode = FilterMode.Bilinear;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Material EnsureWaterMaterial(
            string path,
            Texture2D coverTexture,
            float mainTiling,
            float overlayTiling,
            float blendFactor,
            float overlayStrength,
            float foamStrength)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(WaterShaderName);
            Texture2D mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(MainTexturePath);
            Texture2D overlayTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(OverlayTexturePath);
            if (shader == null || mainTexture == null || overlayTexture == null || coverTexture == null)
            {
                return material;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_MainTex", mainTexture);
            material.SetTexture("_OverlayTex", overlayTexture);
            material.SetTexture("_Texture2DCover", coverTexture);
            material.SetColor("_Color", Color.white);
            material.SetFloat("_brightness", 1f);
            material.SetFloat("_BlendFactor", blendFactor);
            material.SetVector("_MainFlow", new Vector4(0.006f, 0.003f, 0f, 0f));
            material.SetVector("_OverlayFlow", new Vector4(-0.011f, 0.007f, 0f, 0f));
            material.SetFloat("_MainTiling", mainTiling);
            material.SetFloat("_OverlayTiling", overlayTiling);
            material.SetFloat("_OverlayStrength", overlayStrength);
            material.SetFloat("_DistortionStrength", 0.014f);
            material.SetFloat("_MaskLow", 0.04f);
            material.SetFloat("_MaskHigh", 0.40f);
            material.SetFloat("_FoamBand", 0.18f);
            material.SetFloat("_FoamStrength", foamStrength);
            material.SetFloat("_FoamBrightness", 1.25f);
            material.SetFloat("_ShoreColorStrength", coverTexture == null ? 0f : 0.72f);
            material.SetColor("_ShoreColor", new Color(0.36f, 0.79f, 0.82f, 1f));
            material.SetColor("_FoamColor", new Color(0.90f, 0.98f, 0.98f, 1f));
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureBackdropMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BackdropMaterialPath);
            Shader shader = Shader.Find(UnlitShaderName);
            if (shader == null)
            {
                return material;
            }

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, BackdropMaterialPath);
            }

            material.shader = shader;
            material.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            int separatorIndex = assetPath.LastIndexOf('/');
            string parent = assetPath.Substring(0, separatorIndex);
            string folderName = assetPath.Substring(separatorIndex + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
