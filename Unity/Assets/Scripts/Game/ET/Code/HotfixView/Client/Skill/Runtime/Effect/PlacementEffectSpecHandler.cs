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
            SpecExecutionContext context = GetContext();
            if (context != null)
            {
                SpecExecutionContext effectContext = context.CreateOwnedEffectContext(Spec);
                if (effectContext != null)
                {
                    Spec.Context = effectContext;
                }
            }
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
            this.UpdatePlacementLogic();
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
            selfSpec.IsLogicActive = true;
            selfSpec.RuntimePosition = position;
            selfSpec.CurrentTargets.Clear();
            SkillDiagFileLogger.Log($"[PlacementEffect] Start skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} pos={position}");
            SpawnPlacementViewAsync(position).Forget();
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
            selfSpec.IsLogicActive = false;
            this.TriggerPlacementExitForAll();

            if (Spec != null && !Spec.IsDisposed)
            {
                SkillDiagFileLogger.Log($"[PlacementEffect] Cancel skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid}");
                Spec.CancelEffect();
            }
        }

        private async UniTaskVoid SpawnPlacementViewAsync(Vector3 position)
        {
            PlacementEffectNodeData nodeData = GetNode();
            PlacementEffectSpec selfSpec = SelfSpec();
            if (nodeData == null || selfSpec == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            this.CancelPlacementView();

            if (nodeData.placementEntityId <= 0 && string.IsNullOrWhiteSpace(nodeData.placementPrefabPath))
            {
                SkillDiagFileLogger.Log($"[PlacementEffect] NoView skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid}");
                return;
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
                SkillDiagFileLogger.Log($"[PlacementEffect] ViewSpawnFailed skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid}");

                return;
            }

            if (Spec == null || Spec.IsDisposed || placementEntity.IsDisposed)
            {
                return;
            }
        }
    }
}
