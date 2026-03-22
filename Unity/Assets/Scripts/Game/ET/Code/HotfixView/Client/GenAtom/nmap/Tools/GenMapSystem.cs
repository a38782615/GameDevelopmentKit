using System;
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
            self.BiomeMap = new BiomeMap(new float2(self.Width, self.Height));
            self.BiomeMap.SetPointNum(self.PointNum);
            if (self.TxtTexture == null)
            {
                self.BiomeMap.Init(self.MapSeed);
            }
            else
            {
                self.BiomeMap.Init(self.MapSeed, self.CheckIsland);
            }

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

        public static bool CheckIsland(this GenMap self, float2 q)
        {
            int x = Convert.ToInt32(q.x / self.Width * self.TxtWidth);
            int y = Convert.ToInt32(q.y / self.Height * self.TxtHeight);
            x = math.clamp(x, 0, self.TxtTexture.width - 1);
            y = math.clamp(y, 0, self.TxtTexture.height - 1);
            Color textureColor = self.TxtTexture.GetPixel(x, y);
            if (self.IsLake)
            {
                return textureColor != Color.white;
            }

            return textureColor == Color.white;
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
