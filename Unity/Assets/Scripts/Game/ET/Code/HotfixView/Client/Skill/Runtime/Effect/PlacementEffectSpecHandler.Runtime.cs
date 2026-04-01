using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public partial class PlacementEffectSpecHandler
    {
        private void UpdatePlacementLogic()
        {
            PlacementEffectSpec selfSpec = SelfSpec();
            PlacementEffectNodeData nodeData = GetNode();
            if (selfSpec == null || nodeData == null)
            {
                return;
            }

            if (Spec.IsRemoved || Spec.IsCancelled || Spec.IsExpired)
            {
                selfSpec.IsLogicActive = false;
                TriggerPlacementExitForAll();
                CancelPlacementView();
                return;
            }

            if (!Spec.IsApplied || !selfSpec.IsLogicActive || !nodeData.enableCollision)
            {
                return;
            }

            BodyCheckComponent bodyCheck = GetPlacementBodyCheckComponent();
            if (bodyCheck == null)
            {
                return;
            }

            float2 center = global::ET.ModeDefine.Is2D
                ? new float2(selfSpec.RuntimePosition.x, selfSpec.RuntimePosition.y)
                : new float2(selfSpec.RuntimePosition.x, selfSpec.RuntimePosition.z);
            using ListComponent<EntityRef<EntityBody>> bodies = ListComponent<EntityRef<EntityBody>>.Create();
            bodyCheck.SearchCircle(center, nodeData.collisionRadius, bodies);
            using HashSetComponent<long> targetsInRange = HashSetComponent<long>.Create();

            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent target = bodyRef.As()?.GetAbilitySystem();
                if (target == null || target.IsDisposed || target == Spec.Source.As())
                {
                    continue;
                }

                if (!IsPlacementValidTarget(target))
                {
                    continue;
                }

                targetsInRange.Add(target.InstanceId);
                if (selfSpec.CurrentTargets.ContainsKey(target.InstanceId))
                {
                    continue;
                }

                selfSpec.CurrentTargets[target.InstanceId] = target;
                SkillDiagFileLogger.Log($"[PlacementEffect] Enter skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} target={target.InstanceId}");
                TriggerPlacementFlow(target, SkillPortId.PlacementEffect.OnEnter);
            }

            using ListComponent<long> exitedTargets = ListComponent<long>.Create();
            foreach ((long targetId, EntityRef<AbilitySystemComponent> _) in selfSpec.CurrentTargets)
            {
                if (!targetsInRange.Contains(targetId))
                {
                    exitedTargets.Add(targetId);
                }
            }

            foreach (long targetId in exitedTargets)
            {
                AbilitySystemComponent target = selfSpec.CurrentTargets[targetId].As();
                selfSpec.CurrentTargets.Remove(targetId);
                SkillDiagFileLogger.Log($"[PlacementEffect] Exit skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} target={target?.InstanceId ?? 0}");
                TriggerPlacementFlow(target, SkillPortId.PlacementEffect.OnExit);
            }
        }

        private void TriggerPlacementExitForAll()
        {
            PlacementEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null || selfSpec.CurrentTargets.Count == 0)
            {
                return;
            }

            using ListComponent<AbilitySystemComponent> targets = ListComponent<AbilitySystemComponent>.Create();
            foreach (EntityRef<AbilitySystemComponent> targetRef in selfSpec.CurrentTargets.Values)
            {
                AbilitySystemComponent target = targetRef.As();
                if (target != null)
                {
                    targets.Add(target);
                }
            }

            selfSpec.CurrentTargets.Clear();
            foreach (AbilitySystemComponent target in targets)
            {
                SkillDiagFileLogger.Log($"[PlacementEffect] Exit skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} target={target.InstanceId}");
                TriggerPlacementFlow(target, SkillPortId.PlacementEffect.OnExit);
            }
        }

        private void TriggerPlacementFlow(AbilitySystemComponent target, int outputPortId)
        {
            if (target == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            SpecExecutionContext childContext = GetContext()?.CreateWithParentInput(target);
            if (childContext == null)
            {
                return;
            }

            try
            {
                childContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, outputPortId);
            }
            finally
            {
                childContext.Dispose();
            }
        }

        private bool IsPlacementValidTarget(AbilitySystemComponent target)
        {
            PlacementEffectNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return false;
            }

            if (!nodeData.collisionTargetTags.IsEmpty && !target.OwnedTags.HasAnyTags(nodeData.collisionTargetTags))
            {
                return false;
            }

            if (!nodeData.collisionExcludeTags.IsEmpty && target.OwnedTags.HasAnyTags(nodeData.collisionExcludeTags))
            {
                return false;
            }

            return true;
        }

        private BodyCheckComponent GetPlacementBodyCheckComponent()
        {
            Unit unit = Spec.Source.As()?.GetParent<SkillUnit>()?.Unit.As();
            return unit?.Scene()?.GetComponent<BodyCheckComponent>();
        }

        private void CancelPlacementView()
        {
            UGFEntityPlacement placementEntity = this.FindPlacementEntity();
            if (placementEntity != null)
            {
                placementEntity.Cancel();
            }
        }

        private UGFEntityPlacement FindPlacementEntity()
        {
            if (Spec?.Children == null)
            {
                return null;
            }

            foreach (Entity child in Spec.Children.Values)
            {
                if (child is UGFEntityPlacement placementEntity)
                {
                    return placementEntity;
                }
            }

            return null;
        }
    }
}
