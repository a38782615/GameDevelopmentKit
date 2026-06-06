using System;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;
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
            this.UpdateProjectileLogic(deltaTime);
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

            float2 launchPosition = ToPlanar(context.GetPosition(nodeData.launchPositionSource, nodeData.launchBindingName));
            float2 targetPosition = ToPlanar(context.GetPosition(nodeData.targetPositionSource, nodeData.targetBindingName));
            selfSpec.ExpectedTargetPosition = targetPosition;

            AbilitySystemComponent targetUnit = null;
            if (nodeData.projectileTargetType == ProjectileTargetType.Unit)
            {
                targetUnit = GetTargetUnitFromPositionSource(nodeData.targetPositionSource);
            }

            float2 direction = math.normalizesafe(targetPosition - launchPosition, new float2(1f, 0f));
            if (nodeData.projectileTargetType == ProjectileTargetType.Position && Mathf.Abs(nodeData.offsetAngle) > 0.01f)
            {
                direction = RotateVector2(direction, -nodeData.offsetAngle);
            }

            selfSpec.HasTriggeredHit = false;
            selfSpec.ExpectedTargetPosition = targetPosition;
            selfSpec.IsLogicActive = true;
            selfSpec.ReachedTarget = false;
            selfSpec.CurrentPosition = launchPosition;
            selfSpec.CurrentDirection = math.lengthsq(direction) > 0.0001f ? math.normalize(direction) : new float2(1f, 0f);
            selfSpec.StartPosition = launchPosition;
            selfSpec.EndPosition = targetPosition;
            selfSpec.TraveledDistance = 0f;
            selfSpec.TotalDistance = math.distance(launchPosition, targetPosition);
            selfSpec.FlightProgress = 0f;
            selfSpec.HitCount = 0;
            selfSpec.BounceCount = 0;
            selfSpec.HitTargetInstanceIds.Clear();
            SpawnProjectileViewAsync().Forget();
        }

        public override void Cancel()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (selfSpec != null)
            {
                selfSpec.IsLogicActive = false;
                this.CancelProjectileView();
            }

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

        private async UniTaskVoid SpawnProjectileViewAsync()
        {
            ProjectileEffectNodeData nodeData = GetNode();
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (nodeData == null || selfSpec == null || Spec == null || Spec.IsDisposed)
            {
                return;
            }

            this.CancelProjectileView();

            if (nodeData.projectileEntityId <= 0 && string.IsNullOrWhiteSpace(nodeData.projectilePrefabPath))
            {
                return;
            }

            ProjectileInitData initData = new ProjectileInitData
            {
                LaunchPosition = ToVector2(selfSpec.StartPosition),
                TargetPosition = ToVector2(selfSpec.EndPosition),
                Direction = ToVector2(selfSpec.CurrentDirection),
                TargetUnit = GetTargetUnitFromPositionSource(nodeData.targetPositionSource),
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
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                if (!projectileEntity.IsDisposed)
                {
                    projectileEntity.Dispose();
                }

                return;
            }
            catch (Exception e)
            {
                Log.Error($"[ProjectileEffect] Spawn projectile failed. skillId={Spec.SkillId} nodeGuid={Spec.NodeGuid} error={e}");
                if (!projectileEntity.IsDisposed)
                {
                    projectileEntity.Dispose();
                }

                return;
            }

            if (Spec == null || Spec.IsDisposed || projectileEntity.IsDisposed)
            {
                return;
            }

            this.SyncProjectileView();
        }

        private float2 RotateVector2(float2 value, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new float2(value.x * cos - value.y * sin, value.x * sin + value.y * cos);
        }

        private static float2 ToPlanar(Vector3 value)
        {
            return global::ET.ModeDefine.Is2D ? new float2(value.x, value.y) : new float2(value.x, value.z);
        }

        private static Vector2 ToVector2(float2 value)
        {
            return new Vector2(value.x, value.y);
        }
    }
}
