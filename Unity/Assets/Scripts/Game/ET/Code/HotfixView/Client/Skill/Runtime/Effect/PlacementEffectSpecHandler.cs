using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(PlacementEffectSpec))]
    public partial class PlacementEffectSpecHandler : AEffectHandler
    {
        private const string LegacyPlacementEntityGroupName = "Effect";

        public PlacementEffectSpec SelfSpec()
        {
            if (Spec == null || Spec.IsDisposed)
            {
                return null;
            }

            return Spec.GetComponent<PlacementEffectSpec>();
        }

        public PlacementEffectNodeData GetNode()
        {
            return NodeData as PlacementEffectNodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec?.GetContext();
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void OnCompleteHook()
        {
        }

        public override void OnInitialize()
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void Reset()
        {
            Spec.ResetEffect();
        }

        public override void Tick(float deltaTime)
        {
            Spec.TickEffect(deltaTime);
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            PlacementEffectSpec selfSpec = SelfSpec();
            PlacementEffectNodeData nodeData = GetNode();
            SpecExecutionContext context = GetContext();
            if (selfSpec == null || nodeData == null || context == null)
            {
                return;
            }

            Vector3 position = context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);
            SpawnPlacementAsync(position).Forget();
        }

        public override void Cancel()
        {
            PlacementEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                if (Spec != null && !Spec.IsDisposed)
                {
                    Spec.CancelEffect();
                }

                return;
            }

            UGFEntityPlacement placementEntity = selfSpec.PlacementEntity.As();
            if (placementEntity != null)
            {
                placementEntity.Cancel();
            }

            selfSpec.PlacementEntity = default;

            if (Spec != null && !Spec.IsDisposed)
            {
                Spec.CancelEffect();
            }
        }

        private async UniTaskVoid SpawnPlacementAsync(Vector3 position)
        {
            PlacementEffectNodeData nodeData = GetNode();
            PlacementEffectSpec selfSpec = SelfSpec();
            if (nodeData == null || selfSpec == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            UGFEntityPlacement currentPlacement = selfSpec.PlacementEntity.As();
            if (currentPlacement != null)
            {
                currentPlacement.Cancel();
            }

            PlacementInitData initData = new PlacementInitData
            {
                Position = position,
                EnableCollision = nodeData.enableCollision,
                CollisionRadius = nodeData.collisionRadius,
                CollisionTargetTags = nodeData.collisionTargetTags,
                CollisionExcludeTags = nodeData.collisionExcludeTags,
                SourceASC = Spec.Source
            };

            UGFEntityPlacement placementEntity = Spec.AddChild<UGFEntityPlacement, PlacementInitData>(initData);
            selfSpec.PlacementEntity = placementEntity;

            try
            {
                if (nodeData.placementEntityId > 0)
                {
                    await placementEntity.ShowEntityAsync(nodeData.placementEntityId);
                }
                else if (!string.IsNullOrWhiteSpace(nodeData.placementPrefabPath))
                {
                    await placementEntity.ShowEntityAsync(nodeData.placementPrefabPath, LegacyPlacementEntityGroupName);
                }
                else
                {
                    Log.Warning($"[PlacementEffect] Missing placement entity config. skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid}");
                    placementEntity.Dispose();
                    selfSpec.PlacementEntity = default;
                    Spec.CancelEffect();
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[PlacementEffect] Spawn placement failed. skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} error={e}");
                if (!placementEntity.IsDisposed)
                {
                    placementEntity.Dispose();
                }

                if (selfSpec.PlacementEntity.As() == placementEntity)
                {
                    selfSpec.PlacementEntity = default;
                }

                if (Spec != null && !Spec.IsDisposed)
                {
                    Spec.CancelEffect();
                }

                return;
            }

            if (Spec == null || Spec.IsDisposed || placementEntity.IsDisposed)
            {
                return;
            }
        }
    }
}
