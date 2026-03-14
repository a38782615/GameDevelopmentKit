using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
#if Spine
using Spine.Unity;
#endif

namespace ET.Client.Editor
{
    public static class SkillNodeAssetPathUtility
    {
        public static string CreateExportJson(NodeData node)
        {
            if (node == null)
            {
                return string.Empty;
            }

            JObject jsonObject = JObject.Parse(JsonUtility.ToJson(node));
            WriteRuntimeAssetPath(node, jsonObject);
            return jsonObject.ToString(Formatting.None);
        }

        public static void RestoreEditorAssetReferences(NodeData node)
        {
            switch (node)
            {
                case ParticleCueNodeData particleNode:
                    RestoreAssetReference(ref particleNode.particlePrefab, particleNode.particlePrefabPath);
                    break;
                case SoundCueNodeData soundNode:
                    RestoreAssetReference(ref soundNode.soundClip, soundNode.soundClipPath);
                    break;
                case ProjectileEffectNodeData projectileNode:
                    RestoreAssetReference(ref projectileNode.projectilePrefab, projectileNode.projectilePrefabPath);
                    break;
                case PlacementEffectNodeData placementNode:
                    RestoreAssetReference(ref placementNode.placementPrefab, placementNode.placementPrefabPath);
                    break;
                case AnimationNodeData animationNode:
#if Spine
                    RestoreAssetReference(ref animationNode.skeletonDataAsset, animationNode.skeletonDataAssetPath);
#endif
                    break;
            }
        }

        private static void WriteRuntimeAssetPath(NodeData node, JObject jsonObject)
        {
            switch (node)
            {
                case ParticleCueNodeData particleNode:
                    SetAssetPath(jsonObject, nameof(ParticleCueNodeData.particlePrefab), nameof(ParticleCueNodeData.particlePrefabPath), particleNode.particlePrefab);
                    break;
                case SoundCueNodeData soundNode:
                    SetAssetPath(jsonObject, nameof(SoundCueNodeData.soundClip), nameof(SoundCueNodeData.soundClipPath), soundNode.soundClip);
                    break;
                case ProjectileEffectNodeData projectileNode:
                    SetAssetPath(jsonObject, nameof(ProjectileEffectNodeData.projectilePrefab), nameof(ProjectileEffectNodeData.projectilePrefabPath), projectileNode.projectilePrefab);
                    break;
                case PlacementEffectNodeData placementNode:
                    SetAssetPath(jsonObject, nameof(PlacementEffectNodeData.placementPrefab), nameof(PlacementEffectNodeData.placementPrefabPath), placementNode.placementPrefab);
                    break;
                case AnimationNodeData animationNode:
#if Spine
                    SetAssetPath(jsonObject, nameof(AnimationNodeData.skeletonDataAsset), nameof(AnimationNodeData.skeletonDataAssetPath), animationNode.skeletonDataAsset);
#else
                    jsonObject[nameof(AnimationNodeData.skeletonDataAssetPath)] = animationNode.skeletonDataAssetPath ?? string.Empty;
#endif
                    break;
            }
        }

        private static void RestoreAssetReference<TAsset>(ref TAsset assetField, string assetPath) where TAsset : UnityEngine.Object
        {
            assetField = string.IsNullOrWhiteSpace(assetPath) ? null : AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
        }

        private static void SetAssetPath<TAsset>(JObject jsonObject, string objectFieldName, string pathFieldName, TAsset asset) where TAsset : UnityEngine.Object
        {
            jsonObject[pathFieldName] = asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
            jsonObject[objectFieldName] = null;
        }
    }
}
