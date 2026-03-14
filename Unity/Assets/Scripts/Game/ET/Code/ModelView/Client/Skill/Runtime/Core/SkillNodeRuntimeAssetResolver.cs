using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client
{
    public static class SkillNodeRuntimeAssetResolver
    {
        [StaticField]
        private static readonly Dictionary<string, UnityEngine.Object> LoadedAssets = new Dictionary<string, UnityEngine.Object>();

        public static async UniTask PreloadSkillGraphsAsync(IEnumerable<SkillData> skillGraphs)
        {
            if (skillGraphs == null)
            {
                return;
            }

            foreach (SkillData skillGraph in skillGraphs)
            {
                if (skillGraph?.nodes == null)
                {
                    continue;
                }

                foreach (NodeData node in skillGraph.nodes)
                {
                    await ResolveNodeAssetsAsync(node);
                }
            }
        }

        public static void ClearCache()
        {
            foreach (UnityEngine.Object asset in LoadedAssets.Values)
            {
                if (asset != null && UGFComponent.Instance != null)
                {
                    UGFComponent.Instance.UnloadAsset(asset);
                }
            }

            LoadedAssets.Clear();
        }

        private static async UniTask ResolveNodeAssetsAsync(NodeData node)
        {
            switch (node)
            {
                case ParticleCueNodeData particleNode:
                    particleNode.particlePrefab = await LoadAssetAsync<GameObject>(particleNode.particlePrefabPath);
                    break;
                case SoundCueNodeData soundNode:
                    soundNode.soundClip = await LoadAssetAsync<AudioClip>(soundNode.soundClipPath);
                    break;
                case ProjectileEffectNodeData projectileNode:
                    projectileNode.projectilePrefab = await LoadAssetAsync<GameObject>(projectileNode.projectilePrefabPath);
                    break;
                case PlacementEffectNodeData placementNode:
                    placementNode.placementPrefab = await LoadAssetAsync<GameObject>(placementNode.placementPrefabPath);
                    break;
                case AnimationNodeData animationNode:
#if Spine
                    animationNode.skeletonDataAsset = await LoadAssetAsync<SkeletonDataAsset>(animationNode.skeletonDataAssetPath);
#endif
                    break;
            }
        }

        private static async UniTask<TAsset> LoadAssetAsync<TAsset>(string assetPath) where TAsset : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            if (LoadedAssets.TryGetValue(assetPath, out UnityEngine.Object loadedAsset))
            {
                return loadedAsset as TAsset;
            }

            if (UGFComponent.Instance == null)
            {
                Log.Warning($"[SkillAssetResolver] UGFComponent is null. assetPath={assetPath}");
                return null;
            }

            try
            {
                TAsset asset = await UGFComponent.Instance.LoadAssetAsync<TAsset>(assetPath);
                if (asset != null)
                {
                    LoadedAssets[assetPath] = asset;
                }

                return asset;
            }
            catch (Exception e)
            {
                Log.Error($"[SkillAssetResolver] Load asset failed. path={assetPath} type={typeof(TAsset).Name} error={e}");
                return null;
            }
        }
    }
}
