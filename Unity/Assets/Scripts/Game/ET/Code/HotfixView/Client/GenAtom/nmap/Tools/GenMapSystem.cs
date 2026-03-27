using System;
using System.Diagnostics;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace ET
{
    [FriendOf(typeof(GenMap))]
    [FriendOf(typeof(DrawMap))]
    [EntitySystemOf(typeof(GenMap))]
    public static partial class GenMapSystem
    {
        [EntitySystem]
        private static void Awake(this GenMap self)
        {
        }

        public static async UniTask BuildAsync(this GenMap self)
        {
            self.MapSeed = GenerateRandomSeed();
            self.BiomeMap = new BiomeMap(new float2(self.Width, self.Height));
            self.BiomeMap.SetPointNum(self.PointNum);
            self.BiomeMap.SetLakeThreshold(self.LakeThreshold);
            if (self.TxtTexture == null)
            {
                self.BiomeMap.Init(self.MapSeed, self.CreateDefaultIslandShape());
            }
            else
            {
                self.BiomeMap.Init(self.MapSeed, self.CheckIsland);
            }

            self.BiomeMap.MapGraph?.ApplyClusterBiomes(self.MapSeed);

            NoisyEdges noisyEdge = new NoisyEdges(self.MapSeed);
            noisyEdge.BuildNoisyEdges(self.BiomeMap);

            DrawMap drawMap = self.DrawMap.As();
            if (drawMap == null)
            {
                drawMap = self.AddChild<DrawMap>();
                self.DrawMap = drawMap;
            }

            drawMap.View = self.FindMapRoot();
            if (drawMap.View == null)
            {
                Scene currentScene = self.GetParent<Scene>();
                Log.Error($"nmap build failed, cannot find Map root in scene: {currentScene?.Name}");
                return;
            }

            await drawMap.InitAsync();
            drawMap.GenMap(self.BiomeMap, self.RenderWidth, self.RenderHeight);
        }

        public static bool TryEraseGrassAtScreenPoint(this GenMap self, Camera camera, Vector2 screenPosition)
        {
            DrawMap drawMap = self.DrawMap.As();
            return drawMap != null && drawMap.TryEraseGrassAtScreenPoint(camera, screenPosition);
        }

        private static uint GenerateRandomSeed()
        {
            long utcTicks = DateTime.UtcNow.Ticks;
            long stopwatchTicks = Stopwatch.GetTimestamp();
            int environmentTick = Environment.TickCount;
            uint seed = (uint)(utcTicks ^ (utcTicks >> 32) ^ stopwatchTicks ^ (stopwatchTicks >> 32) ^ environmentTick);
            return seed == 0 ? 1u : seed;
        }

        public static bool CheckIsland(this GenMap self, float2 q)
        {
            int x = Convert.ToInt32(q.x / self.Width * self.TxtWidth);
            int y = Convert.ToInt32(q.y / self.Height * self.TxtHeight);
            x = math.clamp(x, 0, self.TxtTexture.width - 1);
            y = math.clamp(y, 0, self.TxtTexture.height - 1);
            Color textureColor = self.TxtTexture.GetPixel(x, y);
            if (self.LakeThreshold > 0)
            {
                return textureColor != Color.white;
            }

            return textureColor == Color.white;
        }
        // 默认随机地图的水陆判定函数。
        // 传入逻辑坐标 q，返回 true 表示陆地，false 表示水域。
        // 整体流程是：先构造大陆主体，再打碎海岸线，最后只在腹地按参数挖湖。
        private static Func<float2, bool> CreateDefaultIslandShape(this GenMap self)
        {
            // 防止宽高异常时出现除以 0。
            float width = math.max(1f, self.Width);
            float height = math.max(1f, self.Height);
            Func<float2, bool> GetFun = (q) =>
            {
                // 不同 seed 使用不同噪声偏移，但同一 seed 的结果保持稳定。
                float2 seedOffset = new float2(
                    (self.MapSeed % 997u) * 0.0137f + 7.13f,
                    ((self.MapSeed / 997u) % 991u) * 0.0179f + 11.29f);
                // 把逻辑坐标归一化到 [-1, 1]，方便统一描述“中心”和“边缘”。
                float2 normalized = new float2(q.x / width * 2f - 1f, q.y / height * 2f - 1f);
                // edgeDistance 反映这个点离矩形边界有多近，越靠边越大。
                // radialDistance 反映这个点离地图中心有多远，越靠中心越小。
                float edgeDistance = math.max(math.abs(normalized.x), math.abs(normalized.y));
                float radialDistance = math.length(normalized);
                // 低频噪声决定大陆整体轮廓，中频噪声把海岸线打散得更自然。
                float continentNoise = Perlin.Fbm(normalized * 1.8f + seedOffset, 4) * 0.24f;
                float coastNoise = Perlin.Fbm(normalized * 3.2f + seedOffset * 1.9f + new float2(13.1f, 5.7f), 3) * 0.12f;

                // 先构造大陆主体，让边缘更容易收束成海岸。
                float landScore = 0.6f - edgeDistance * 0.48f - radialDistance * 0.16f + continentNoise + coastNoise;
                return landScore > 0.1;
            };
            return GetFun;
        }

        private static GameObject FindMapRoot(this GenMap self)
        {
            string sceneName = self.GetParent<Scene>()?.Name;
            if (!string.IsNullOrEmpty(sceneName))
            {
                UnityScene unityScene = UnitySceneManager.GetSceneByName(sceneName);
                if (unityScene.IsValid() && unityScene.isLoaded)
                {
                    foreach (GameObject rootObject in unityScene.GetRootGameObjects())
                    {
                        if (rootObject.name == "Map")
                        {
                            return rootObject;
                        }
                    }
                }
            }

            return GameObject.Find("Map");
        }
    }
}
