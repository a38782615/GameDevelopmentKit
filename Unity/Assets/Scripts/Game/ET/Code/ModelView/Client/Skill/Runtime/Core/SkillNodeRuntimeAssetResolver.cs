using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Game;
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
                    if (particleNode.particleEntityId > 0)
                    {
                        await PreloadEntityPrefabAsync(particleNode.particleEntityId);
                    }
                    break;
                case SoundCueNodeData soundNode:
                    soundNode.soundClip = await LoadAssetOrKeepAsync(soundNode.soundClip, soundNode.soundClipPath);
                    break;
                case ProjectileEffectNodeData projectileNode:
                    if (projectileNode.projectileEntityId <= 0)
                    {
                        projectileNode.projectilePrefab = await LoadAssetOrKeepAsync(projectileNode.projectilePrefab, projectileNode.projectilePrefabPath);
                    }
                    break;
                case PlacementEffectNodeData placementNode:
                    placementNode.placementPrefab = await LoadAssetOrKeepAsync(placementNode.placementPrefab, placementNode.placementPrefabPath);
                    break;
                case AnimationNodeData animationNode:
#if Spine
                    animationNode.skeletonDataAsset = await LoadAssetOrKeepAsync(animationNode.skeletonDataAsset, animationNode.skeletonDataAssetPath);
#endif
                    break;
            }
        }

        public static GameObject GetEntityPrefab(int entityId)
        {
            if (entityId <= 0)
            {
                return null;
            }

            DREntity drEntity = GameEntry.Tables?.DTEntity?.GetOrDefault(entityId);
            if (drEntity == null)
            {
                return null;
            }

            string assetPath = AssetUtility.GetEntityAsset(drEntity.AssetName);
            return LoadedAssets.TryGetValue(assetPath, out UnityEngine.Object loadedAsset) ? loadedAsset as GameObject : null;
        }

        private static async UniTask<TAsset> LoadAssetOrKeepAsync<TAsset>(TAsset currentAsset, string assetPath) where TAsset : UnityEngine.Object
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return currentAsset;
            }

            return await LoadAssetAsync<TAsset>(assetPath);
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

        private static async UniTask PreloadEntityPrefabAsync(int entityId)
        {
            DREntity drEntity = GameEntry.Tables?.DTEntity?.GetOrDefault(entityId);
            if (drEntity == null)
            {
                Log.Warning($"[SkillAssetResolver] Entity config not found. entityId={entityId}");
                return;
            }

            string assetPath = AssetUtility.GetEntityAsset(drEntity.AssetName);
            await LoadAssetAsync<GameObject>(assetPath);
        }
    }
}
