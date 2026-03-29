using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    public partial class ProjectileEffectSpecHandler
    {
        private void UpdateProjectileLogic(float deltaTime)
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            ProjectileEffectNodeData nodeData = GetNode();
            if (selfSpec == null || nodeData == null)
            {
                return;
            }

            if (Spec.IsRemoved || Spec.IsCancelled || Spec.IsExpired)
            {
                selfSpec.IsLogicActive = false;
                CancelProjectileView();
                return;
            }

            if (!Spec.IsApplied || !selfSpec.IsLogicActive)
            {
                return;
            }

            float moveDistance = nodeData.speed * deltaTime;
            selfSpec.TraveledDistance += moveDistance;

            if (nodeData.projectileTargetType == ProjectileTargetType.Unit)
            {
                AbilitySystemComponent targetUnit = GetTargetUnitFromPositionSource(nodeData.targetPositionSource);
                if (targetUnit != null)
                {
                    selfSpec.EndPosition = GetAbilityPosition(targetUnit, nodeData.targetBindingName, selfSpec.EndPosition);
                    selfSpec.TotalDistance = Vector2.Distance(selfSpec.CurrentPosition, selfSpec.EndPosition);
                }

                if (nodeData.curveHeight > 0f && selfSpec.TotalDistance > 0.1f)
                {
                    selfSpec.FlightProgress = Mathf.Clamp01(selfSpec.TraveledDistance / (selfSpec.TotalDistance + selfSpec.TraveledDistance));
                    Vector2 linearPos = Vector2.MoveTowards(selfSpec.CurrentPosition, selfSpec.EndPosition, moveDistance);
                    float curveOffset = nodeData.curveHeight * 4f * selfSpec.FlightProgress * (1f - selfSpec.FlightProgress);
                    Vector2 perpendicular = new Vector2(selfSpec.CurrentDirection.y, -selfSpec.CurrentDirection.x);
                    selfSpec.CurrentPosition = linearPos + perpendicular * curveOffset;
                    selfSpec.CurrentDirection = (selfSpec.EndPosition - selfSpec.CurrentPosition).normalized;
                }
                else
                {
                    selfSpec.CurrentDirection = (selfSpec.EndPosition - selfSpec.CurrentPosition).normalized;
                    selfSpec.CurrentPosition = Vector2.MoveTowards(selfSpec.CurrentPosition, selfSpec.EndPosition, moveDistance);
                }
            }
            else if (nodeData.flyOver)
            {
                selfSpec.CurrentPosition += selfSpec.CurrentDirection * moveDistance;
            }
            else if (nodeData.curveHeight > 0f && selfSpec.TotalDistance > 0.1f)
            {
                selfSpec.FlightProgress = Mathf.Clamp01(selfSpec.TraveledDistance / selfSpec.TotalDistance);
                Vector2 linearPos = Vector2.Lerp(selfSpec.StartPosition, selfSpec.EndPosition, selfSpec.FlightProgress);
                Vector2 forward = (selfSpec.EndPosition - selfSpec.StartPosition).normalized;
                Vector2 perpendicular = new Vector2(forward.y, -forward.x);
                float curveOffset = nodeData.curveHeight * 4f * selfSpec.FlightProgress * (1f - selfSpec.FlightProgress);
                selfSpec.CurrentPosition = linearPos + perpendicular * curveOffset;
            }
            else
            {
                selfSpec.CurrentPosition += selfSpec.CurrentDirection * moveDistance;
            }

            CheckProjectileCollision();
            if (!selfSpec.IsLogicActive)
            {
                return;
            }

            CheckProjectileReachTarget();
            SyncProjectileView();
        }

        private void CheckProjectileCollision()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            ProjectileEffectNodeData nodeData = GetNode();
            BodyCheckComponent bodyCheck = GetBodyCheckComponent();
            if (selfSpec == null || nodeData == null || bodyCheck == null)
            {
                return;
            }

            using ListComponent<EntityRef<EntityBody>> bodies = ListComponent<EntityRef<EntityBody>>.Create();
            bodyCheck.SearchCircle(new float2(selfSpec.CurrentPosition.x, selfSpec.CurrentPosition.y), nodeData.collisionRadius, bodies);
            AbilitySystemComponent sourceAsc = Spec.Source.As();

            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent target = bodyRef.As()?.GetAbilitySystem();
                if (target == null || target == sourceAsc || target.IsDisposed || selfSpec.HitTargetInstanceIds.Contains(target.InstanceId))
                {
                    continue;
                }

                if (!IsProjectileValidTarget(target))
                {
                    continue;
                }

                selfSpec.HitTargetInstanceIds.Add(target.InstanceId);
                selfSpec.HitCount++;
                TriggerProjectileHit(target, selfSpec.CurrentPosition);
                if (!selfSpec.IsLogicActive)
                {
                    return;
                }

                if (!nodeData.isPiercing)
                {
                    if (nodeData.isBouncing && selfSpec.BounceCount < nodeData.maxBounceCount && TryBounceTarget(target))
                    {
                        return;
                    }

                    DestroyProjectileLogic();
                    return;
                }

                if (selfSpec.HitCount >= nodeData.maxPierceCount)
                {
                    if (nodeData.isBouncing && selfSpec.BounceCount < nodeData.maxBounceCount && TryBounceTarget(target))
                    {
                        selfSpec.HitCount = 0;
                        return;
                    }

                    DestroyProjectileLogic();
                    return;
                }
            }
        }

        private void CheckProjectileReachTarget()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            ProjectileEffectNodeData nodeData = GetNode();
            if (selfSpec == null || nodeData == null)
            {
                return;
            }

            if (nodeData.projectileTargetType == ProjectileTargetType.Unit)
            {
                if (GetTargetUnitFromPositionSource(nodeData.targetPositionSource) == null && nodeData.maxDistance > 0f && selfSpec.TraveledDistance >= nodeData.maxDistance)
                {
                    DestroyProjectileLogic();
                }
                return;
            }

            if (!selfSpec.ReachedTarget)
            {
                float distToTarget = Vector2.Distance(selfSpec.CurrentPosition, selfSpec.EndPosition);
                if (distToTarget < nodeData.collisionRadius || (selfSpec.TotalDistance > 0f && selfSpec.TraveledDistance >= selfSpec.TotalDistance))
                {
                    selfSpec.ReachedTarget = true;
                    SkillDiagFileLogger.Log($"[ProjectileEffect] Reach skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} pos={selfSpec.EndPosition}");
                    GetContext()?.SetCustomData("ReachPosition", selfSpec.EndPosition);
                    GetContext()?.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnReachTarget);
                }
            }

            if (nodeData.maxDistance > 0f && selfSpec.TraveledDistance >= nodeData.maxDistance)
            {
                DestroyProjectileLogic();
            }
            else if (!nodeData.flyOver && (selfSpec.FlightProgress >= 1f || Vector2.Distance(selfSpec.CurrentPosition, selfSpec.EndPosition) < 0.1f))
            {
                DestroyProjectileLogic();
            }
        }

        private bool TryBounceTarget(AbilitySystemComponent currentTarget)
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            ProjectileEffectNodeData nodeData = GetNode();
            BodyCheckComponent bodyCheck = GetBodyCheckComponent();
            if (selfSpec == null || nodeData == null || bodyCheck == null)
            {
                return false;
            }

            selfSpec.BounceCount++;
            using ListComponent<EntityRef<EntityBody>> bodies = ListComponent<EntityRef<EntityBody>>.Create();
            bodyCheck.SearchCircle(new float2(selfSpec.CurrentPosition.x, selfSpec.CurrentPosition.y), nodeData.bounceSearchRadius, bodies);
            AbilitySystemComponent nextTarget = null;
            float nearest = float.MaxValue;
            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent candidate = bodyRef.As()?.GetAbilitySystem();
                if (candidate == null || candidate == currentTarget || candidate == Spec.Source.As() || candidate.IsDisposed)
                {
                    continue;
                }

                if (!nodeData.canBounceToSameTarget && selfSpec.HitTargetInstanceIds.Contains(candidate.InstanceId))
                {
                    continue;
                }

                if (!IsProjectileValidTarget(candidate))
                {
                    continue;
                }

                float distance = Vector2.Distance(selfSpec.CurrentPosition, GetAbilityPosition(candidate, nodeData.targetBindingName, selfSpec.CurrentPosition));
                if (distance < nearest)
                {
                    nearest = distance;
                    nextTarget = candidate;
                }
            }

            if (nextTarget == null)
            {
                selfSpec.BounceCount--;
                return false;
            }

            if (!nodeData.canBounceToSameTarget && currentTarget != null)
            {
                selfSpec.HitTargetInstanceIds.Add(currentTarget.InstanceId);
            }

            selfSpec.StartPosition = selfSpec.CurrentPosition;
            selfSpec.EndPosition = GetAbilityPosition(nextTarget, nodeData.targetBindingName, selfSpec.EndPosition);
            selfSpec.TotalDistance = Vector2.Distance(selfSpec.StartPosition, selfSpec.EndPosition);
            selfSpec.TraveledDistance = 0f;
            selfSpec.FlightProgress = 0f;
            selfSpec.CurrentDirection = (selfSpec.EndPosition - selfSpec.StartPosition).normalized;
            SkillDiagFileLogger.Log($"[ProjectileEffect] Bounce skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} nextTarget={nextTarget.InstanceId} pos={selfSpec.CurrentPosition}");
            SpecExecutionContext bounceContext = GetContext()?.CreateWithParentInput(nextTarget);
            if (bounceContext != null)
            {
                try
                {
                    bounceContext.SetCustomData("BouncePosition", selfSpec.CurrentPosition);
                    bounceContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnBounce);
                }
                finally
                {
                    bounceContext.Dispose();
                }
            }

            return true;
        }

        private void TriggerProjectileHit(AbilitySystemComponent target, Vector2 hitPosition)
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            SpecExecutionContext hitContext = GetContext()?.CreateWithParentInput(target);
            if (selfSpec == null || hitContext == null)
            {
                return;
            }

            selfSpec.HasTriggeredHit = true;
            SkillDiagFileLogger.Log($"[ProjectileEffect] Hit skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} target={target.InstanceId} pos={hitPosition}");
            try
            {
                hitContext.SetCustomData("HitPosition", hitPosition);
                hitContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnHit);
            }
            finally
            {
                hitContext.Dispose();
            }
        }

        private bool IsProjectileValidTarget(AbilitySystemComponent target)
        {
            ProjectileEffectNodeData nodeData = GetNode();
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

        private Vector2 GetAbilityPosition(AbilitySystemComponent asc, string bindingName, Vector2 fallback)
        {
            if (asc?.Owner != null)
            {
                Transform transform = asc.Owner.transform;
                if (!string.IsNullOrEmpty(bindingName))
                {
                    Transform binding = transform.Find(bindingName);
                    if (binding != null)
                    {
                        return binding.position;
                    }
                }
                return transform.position;
            }

            Unit unit = asc?.GetParent<SkillUnit>()?.Unit.As();
            return unit == null ? fallback : new Vector2(unit.Position.x, unit.Position.y);
        }

        private BodyCheckComponent GetBodyCheckComponent()
        {
            Unit unit = Spec.Source.As()?.GetParent<SkillUnit>()?.Unit.As();
            return unit?.Scene()?.GetComponent<BodyCheckComponent>();
        }

        private void DestroyProjectileLogic()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            ProjectileEffectNodeData nodeData = GetNode();
            if (selfSpec == null || !selfSpec.IsLogicActive)
            {
                return;
            }

            if (!selfSpec.HasTriggeredHit && nodeData != null && nodeData.projectileTargetType == ProjectileTargetType.Position)
            {
                AbilitySystemComponent target = GetContext()?.GetMainTarget();
                if (target != null && Vector2.Distance(selfSpec.CurrentPosition, selfSpec.ExpectedTargetPosition) <= Mathf.Max(nodeData.collisionRadius, 0.75f))
                {
                    TriggerProjectileHit(target, selfSpec.CurrentPosition);
                }
            }

            selfSpec.IsLogicActive = false;
            CancelProjectileView();
            SkillDiagFileLogger.Log($"[ProjectileEffect] Destroy skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} pos={selfSpec.CurrentPosition}");
            Spec.RemoveEffect();
        }

        private void SyncProjectileView()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            UGFEntityProjectile projectileEntity = selfSpec?.ProjectileEntity.As();
            if (selfSpec == null || projectileEntity?.CachedTransform == null)
            {
                return;
            }

            projectileEntity.CachedTransform.position = selfSpec.CurrentPosition;
            if (selfSpec.CurrentDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(selfSpec.CurrentDirection.y, selfSpec.CurrentDirection.x) * Mathf.Rad2Deg;
                projectileEntity.CachedTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void CancelProjectileView()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            UGFEntityProjectile projectileEntity = selfSpec?.ProjectileEntity.As();
            if (projectileEntity != null)
            {
                projectileEntity.Cancel();
            }

            if (selfSpec != null)
            {
                selfSpec.ProjectileEntity = default;
            }
        }
    }
}
