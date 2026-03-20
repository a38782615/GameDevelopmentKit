using UnityEngine;

namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.ProjectileEffectSpec))]
    public partial class ProjectileEffectSpecHandler : AEffectHandler
    {
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
            if (nodeData == null)
            {
                return;
            }

            ProjectileEffectSpec selfSpec = SelfSpec();
            SpecExecutionContext context = GetContext();
            if (selfSpec == null || context == null)
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

            SpawnProjectile(launchPosition, targetPosition, direction, targetUnit);

            context.SetProjectileObject(selfSpec._projectileObject);
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

        private void SpawnProjectile(Vector2 launchPosition, Vector2 targetPosition, Vector2 direction, AbilitySystemComponent targetUnit)
        {
            ProjectileEffectNodeData nodeData = GetNode();
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null)
            {
                return;
            }

            if (nodeData.projectilePrefab != null)
            {
                selfSpec._projectileObject = UnityEngine.Object.Instantiate(nodeData.projectilePrefab, launchPosition, Quaternion.identity);
            }
            else
            {
                selfSpec._projectileObject = new GameObject("Projectile");
                selfSpec._projectileObject.transform.position = launchPosition;
            }

            ProjectileController controller = selfSpec._projectileObject.GetComponent<ProjectileController>();
            if (controller == null)
            {
                controller = selfSpec._projectileObject.AddComponent<ProjectileController>();
            }

            controller.Initialize(new ProjectileInitData
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
                SkillId = Spec.SkillId,
                NodeGuid = Spec.NodeGuid,
                Context = Spec.GetContext(),
                SourceASC = Spec.Source,
                IsBouncing = nodeData.isBouncing,
                BounceTargetMode = nodeData.bounceTargetMode,
                MaxBounceCount = nodeData.maxBounceCount,
                BounceSearchRadius = nodeData.bounceSearchRadius,
                CanBounceToSameTarget = nodeData.canBounceToSameTarget,
                ExcludeSourceCamp = nodeData.excludeSourceCamp,
                BounceAngleOffset = nodeData.bounceAngleOffset
            });

            selfSpec._projectileController = controller;
            controller.OnHit += OnProjectileHit;
            controller.OnReachTarget += OnProjectileReachTarget;
            controller.OnBounce += OnProjectileBounce;
            controller.OnDestroy += OnProjectileDestroy;
        }

        private void OnProjectileHit(AbilitySystemComponent hitTarget, Vector2 hitPosition)
        {
            if (hitTarget == null)
            {
                return;
            }

            SpecExecutionContext context = GetContext();
            ProjectileEffectSpec selfSpec = SelfSpec();

            if (context == null || selfSpec == null)
            {
                return;
            }

            selfSpec.HasTriggeredHit = true;
            SpecExecutionContext hitContext = context.CreateWithParentInput(hitTarget);

            if (hitContext == null)
            {
                return;
            }

            try
            {
                hitContext.SetCustomData("HitPosition", hitPosition);
                hitContext.SetProjectileObject(selfSpec._projectileObject);
                hitContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnHit);
            }
            finally
            {
                hitContext.Dispose();
            }
        }

        private void OnProjectileReachTarget(Vector2 position)
        {
            SpecExecutionContext ctx = GetExecutionContext();
            ProjectileEffectSpec selfSpec = SelfSpec();

            if (ctx == null || selfSpec == null)
            {
                return;
            }

            TryTriggerPositionFallbackHit(selfSpec, position);
            ctx.SetCustomData("ReachPosition", position);
            ctx.SetProjectileObject(selfSpec._projectileObject);
            ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnReachTarget);
        }

        private void OnProjectileBounce(AbilitySystemComponent nextTarget, Vector2 bouncePosition)
        {
            if (nextTarget == null)
            {
                return;
            }

            SpecExecutionContext context = GetContext();
            ProjectileEffectSpec selfSpec = SelfSpec();
            if (context == null || selfSpec == null)
            {
                return;
            }

            SpecExecutionContext bounceContext = context.CreateWithParentInput(nextTarget);
            if (bounceContext == null)
            {
                return;
            }

            try
            {
                bounceContext.SetCustomData("BouncePosition", bouncePosition);
                bounceContext.SetProjectileObject(selfSpec._projectileObject);
                bounceContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.ProjectileEffect.OnBounce);
            }
            finally
            {
                bounceContext.Dispose();
            }
        }

        private void OnProjectileDestroy()
        {
            ProjectileEffectSpec selfSpec = SelfSpec();
            Vector2? projectilePosition = null;
            if (selfSpec?._projectileObject != null)
            {
                projectilePosition = selfSpec._projectileObject.transform.position;
            }

            TryTriggerPositionFallbackHit(selfSpec, projectilePosition);

            if (selfSpec != null)
            {
                selfSpec._projectileController = null;
                selfSpec._projectileObject = null;
            }

            if (Spec != null && !Spec.IsDisposed)
            {
                Spec.RemoveEffect();
            }
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

            if (selfSpec._projectileController != null)
            {
                selfSpec._projectileController.OnHit -= OnProjectileHit;
                selfSpec._projectileController.OnReachTarget -= OnProjectileReachTarget;
                selfSpec._projectileController.OnBounce -= OnProjectileBounce;
                selfSpec._projectileController.OnDestroy -= OnProjectileDestroy;

                if (selfSpec._projectileController.gameObject != null)
                {
                    UnityEngine.Object.Destroy(selfSpec._projectileController.gameObject);
                }

                selfSpec._projectileController = null;
                selfSpec._projectileObject = null;
            }

            Spec.CancelEffect();
        }

        private Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void TryTriggerPositionFallbackHit(ProjectileEffectSpec selfSpec, Vector2? projectilePosition)
        {
            ProjectileEffectNodeData nodeData = GetNode();
            if (selfSpec == null
                || selfSpec.HasTriggeredHit
                || !projectilePosition.HasValue
                || nodeData == null
                || nodeData.projectileTargetType != ProjectileTargetType.Position)
            {
                return;
            }

            float fallbackRadius = Mathf.Max(nodeData.collisionRadius, 0.75f);
            if (Vector2.Distance(projectilePosition.Value, selfSpec.ExpectedTargetPosition) > fallbackRadius)
            {
                return;
            }

            AbilitySystemComponent fallbackTarget = GetContext()?.GetMainTarget();
            if (fallbackTarget == null)
            {
                return;
            }

            selfSpec.HasTriggeredHit = true;
            OnProjectileHit(fallbackTarget, projectilePosition.Value);
        }
    }
}
