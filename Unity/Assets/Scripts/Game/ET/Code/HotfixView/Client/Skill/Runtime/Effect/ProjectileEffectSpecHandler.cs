using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(ProjectileEffectSpec))]
    public partial class ProjectileEffectSpecHandler : AEffectHandler
    {
        private const string LegacyProjectileEntityGroupName = "Effect";

        public ProjectileEffectSpec SelfSpec()
        {
            if (Spec == null || Spec.IsDisposed)
            {
                return null;
            }

            return Spec.GetComponent<ProjectileEffectSpec>();
        }

        public ProjectileEffectNodeData GetNode()
        {
            return NodeData as ProjectileEffectNodeData;
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

            Spec.Duration = -1f;
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            ProjectileEffectNodeData nodeData = GetNode();
            ProjectileEffectSpec selfSpec = SelfSpec();
            SpecExecutionContext context = GetContext();
            if (nodeData == null || selfSpec == null || context == null)
            {
                return;
            }

            selfSpec.HasTriggeredHit = false;

            Vector2 launchPosition = context.GetPosition(nodeData.launchPositionSource, nodeData.launchBindingName);
            Vector2 targetPosition = context.GetPosition(nodeData.targetPositionSource, nodeData.targetBindingName);
            selfSpec.ExpectedTargetPosition = targetPosition;

            AbilitySystemComponent targetUnit = null;
            if (nodeData.projectileTargetType == ProjectileTargetType.Unit)
            {
                targetUnit = GetTargetUnitFromPositionSource(nodeData.targetPositionSource);
            }

            Vector2 direction = (targetPosition - launchPosition).normalized;
            if (nodeData.projectileTargetType == ProjectileTargetType.Position && Mathf.Abs(nodeData.offsetAngle) > 0.01f)
            {
                direction = RotateVector2(direction, -nodeData.offsetAngle);
            }

            SpawnProjectileAsync(launchPosition, targetPosition, direction, targetUnit).Forget();
        }

        public override void Cancel()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                if (Spec != null && !Spec.IsDisposed)
                {
                    Spec.CancelEffect();
                }

                return;
            }

            UGFEntityProjectile projectileEntity = selfSpec.ProjectileEntity.As();
            if (projectileEntity != null)
            {
                projectileEntity.Cancel();
            }

            selfSpec.ProjectileEntity = default;
            selfSpec.ProjectileObject = null;
            GetContext()?.SetProjectileObject(null);

            if (Spec != null && !Spec.IsDisposed)
            {
                Spec.CancelEffect();
            }
        }

        private AbilitySystemComponent GetTargetUnitFromPositionSource(PositionSourceType sourceType)
        {
            SpecExecutionContext context = GetContext();
            switch (sourceType)
            {
                case PositionSourceType.Caster:
                    return context?.GetCaster();
                case PositionSourceType.MainTarget:
                    return context?.GetMainTarget();
                case PositionSourceType.ParentInput:
                    return context?.GetParentInputTarget();
                default:
                    return context?.GetMainTarget();
            }
        }

        private async UniTaskVoid SpawnProjectileAsync(Vector2 launchPosition, Vector2 targetPosition, Vector2 direction, AbilitySystemComponent targetUnit)
        {
            ProjectileEffectNodeData nodeData = GetNode();
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (nodeData == null || selfSpec == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            UGFEntityProjectile currentProjectile = selfSpec.ProjectileEntity.As();
            if (currentProjectile != null)
            {
                currentProjectile.Cancel();
            }

            ProjectileInitData initData = new ProjectileInitData
            {
                LaunchPosition = launchPosition,
                TargetPosition = targetPosition,
                Direction = direction,
                TargetUnit = targetUnit,
                TargetType = nodeData.projectileTargetType,
                FlyOver = nodeData.flyOver,
                CurveHeight = nodeData.curveHeight,
                Speed = nodeData.speed,
                MaxDistance = nodeData.maxDistance,
                CollisionRadius = nodeData.collisionRadius,
                IsPiercing = nodeData.isPiercing,
                MaxPierceCount = nodeData.maxPierceCount,
                CollisionTargetTags = nodeData.collisionTargetTags,
                CollisionExcludeTags = nodeData.collisionExcludeTags,
                TargetBindingName = nodeData.targetBindingName,
                SourceASC = Spec.Source,
                IsBouncing = nodeData.isBouncing,
                BounceTargetMode = nodeData.bounceTargetMode,
                MaxBounceCount = nodeData.maxBounceCount,
                BounceSearchRadius = nodeData.bounceSearchRadius,
                CanBounceToSameTarget = nodeData.canBounceToSameTarget,
                ExcludeSourceCamp = nodeData.excludeSourceCamp,
                BounceAngleOffset = nodeData.bounceAngleOffset
            };

            UGFEntityProjectile projectileEntity = Spec.AddChild<UGFEntityProjectile, ProjectileInitData>(initData);
            selfSpec.ProjectileEntity = projectileEntity;

            try
            {
                if (nodeData.projectileEntityId > 0)
                {
                    await projectileEntity.ShowEntityAsync(nodeData.projectileEntityId);
                }
                else if (!string.IsNullOrWhiteSpace(nodeData.projectilePrefabPath))
                {
                    await projectileEntity.ShowEntityAsync(nodeData.projectilePrefabPath, LegacyProjectileEntityGroupName);
                }
                else
                {
                    Log.Warning($"[ProjectileEffect] Missing projectile entity config. skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid}");
                    projectileEntity.Dispose();
                    selfSpec.ProjectileEntity = default;
                    selfSpec.ProjectileObject = null;
                    GetContext()?.SetProjectileObject(null);
                    Spec.CancelEffect();
                    return;
                }
            }
            catch (Exception e)
            {
                Log.Error($"[ProjectileEffect] Spawn projectile failed. skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} error={e}");
                if (!projectileEntity.IsDisposed)
                {
                    projectileEntity.Dispose();
                }

                if (selfSpec.ProjectileEntity.As() == projectileEntity)
                {
                    selfSpec.ProjectileEntity = default;
                }

                selfSpec.ProjectileObject = null;
                GetContext()?.SetProjectileObject(null);
                if (Spec != null && !Spec.IsDisposed)
                {
                    Spec.CancelEffect();
                }

                return;
            }

            if (Spec == null || Spec.IsDisposed || projectileEntity.IsDisposed)
            {
                return;
            }

            selfSpec.ProjectileObject = projectileEntity.CachedTransform != null
                ? projectileEntity.CachedTransform.gameObject
                : null;
            GetContext()?.SetProjectileObject(selfSpec.ProjectileObject);
        }

        private Vector2 RotateVector2(Vector2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }
    }
}
