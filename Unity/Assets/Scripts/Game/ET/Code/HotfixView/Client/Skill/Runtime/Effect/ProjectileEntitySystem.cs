using UnityEngine;

namespace ET.Client
{
    [FriendOf(typeof(AbilitySystemComponent))]
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(ProjectileEntity))]
    [FriendOf(typeof(ProjectileEffectSpec))]
    [EntitySystemOf(typeof(ProjectileEntity))]
    public static partial class ProjectileEntitySystem
    {
        [EntitySystem]
        private static void Awake(this ProjectileEntity self, ProjectileInitData initData)
        {
            self.InitData = initData;
        }

        [UGFEntitySystem]
        private static void UGFEntityOnShow(this ProjectileEntity self)
        {
            self.InitializeRuntimeState();

            ProjectileEffectSpec projectileSpec = self.GetProjectileSpec();
            if (projectileSpec != null)
            {
                projectileSpec.ProjectileEntity = self;
                projectileSpec.ProjectileObject = self.GetProjectileObject();
            }
        }

        [UGFEntitySystem]
        private static void UGFEntityOnHide(this ProjectileEntity self, bool isShutdown)
        {
            self.Initialized = false;
            self.DestroyRequested = true;
            self.HitTargetInstanceIds.Clear();

            ProjectileEffectSpec projectileSpec = self.GetProjectileSpec();
            GameObject projectileObject = self.GetProjectileObject();
            if (projectileSpec != null)
            {
                if (projectileSpec.ProjectileEntity.As() == self)
                {
                    projectileSpec.ProjectileEntity = default;
                }

                if (projectileSpec.ProjectileObject == projectileObject)
                {
                    projectileSpec.ProjectileObject = null;
                }
            }
        }

        [UGFEntitySystem]
        private static void UGFEntityOnUpdate(this ProjectileEntity self, float elapseSeconds, float realElapseSeconds)
        {
            if (!self.CanContinue())
            {
                return;
            }

            self.UpdatePosition(realElapseSeconds);
            if (!self.CanContinue())
            {
                return;
            }

            self.CheckCollision();
            if (!self.CanContinue())
            {
                return;
            }

            self.CheckReachTarget();
            if (!self.CanContinue())
            {
                return;
            }

            if (self.CachedTransform != null)
            {
                self.CachedTransform.position = self.CurrentPosition;
                self.UpdateRotation();
            }
        }

        public static void Cancel(this ProjectileEntity self)
        {
            if (self == null || self.IsDisposed)
            {
                return;
            }

            self.Initialized = false;
            self.DestroyRequested = true;
            self.Dispose();
        }

        private static void InitializeRuntimeState(this ProjectileEntity self)
        {
            self.Initialized = true;
            self.DestroyRequested = false;
            self.ReachedTarget = false;
            self.CurrentPosition = self.InitData.LaunchPosition;
            self.CurrentDirection = self.InitData.Direction.sqrMagnitude > 0.0001f
                ? self.InitData.Direction.normalized
                : (self.InitData.TargetPosition - self.InitData.LaunchPosition).normalized;
            self.StartPosition = self.InitData.LaunchPosition;
            self.EndPosition = self.InitData.TargetPosition;
            self.TraveledDistance = 0f;
            self.TotalDistance = Vector2.Distance(self.StartPosition, self.EndPosition);
            self.FlightProgress = 0f;
            self.HitCount = 0;
            self.BounceCount = 0;
            self.HitTargetInstanceIds.Clear();

            if (self.CachedTransform != null)
            {
                self.CachedTransform.position = self.CurrentPosition;
                self.UpdateRotation();
            }
        }

        private static bool CanContinue(this ProjectileEntity self)
        {
            return self != null && !self.IsDisposed && self.Initialized && !self.DestroyRequested;
        }

        private static void UpdatePosition(this ProjectileEntity self, float deltaTime)
        {
            float moveDistance = self.InitData.Speed * deltaTime;
            self.TraveledDistance += moveDistance;

            if (self.InitData.TargetType == ProjectileTargetType.Unit)
            {
                self.UpdatePositionForUnit(moveDistance);
                return;
            }

            self.UpdatePositionForPosition(moveDistance);
        }

        private static void UpdatePositionForUnit(this ProjectileEntity self, float moveDistance)
        {
            AbilitySystemComponent targetUnit = self.InitData.TargetUnit.As();
            if (targetUnit?.Owner != null)
            {
                self.EndPosition = self.GetTargetUnitPosition();
                self.TotalDistance = Vector2.Distance(self.CurrentPosition, self.EndPosition);
            }

            if (self.InitData.CurveHeight > 0f && self.TotalDistance > 0.1f)
            {
                self.FlightProgress = Mathf.Clamp01(self.TraveledDistance / (self.TotalDistance + self.TraveledDistance));
                Vector2 linearPos = Vector2.MoveTowards(self.CurrentPosition, self.EndPosition, moveDistance);
                float curveOffset = self.InitData.CurveHeight * 4f * self.FlightProgress * (1f - self.FlightProgress);
                Vector2 perpendicular = self.GetPerpendicular(self.CurrentDirection);
                self.CurrentPosition = linearPos + perpendicular * curveOffset;
                self.CurrentDirection = (self.EndPosition - self.CurrentPosition).normalized;
                return;
            }

            self.CurrentDirection = (self.EndPosition - self.CurrentPosition).normalized;
            self.CurrentPosition = Vector2.MoveTowards(self.CurrentPosition, self.EndPosition, moveDistance);
        }

        private static void UpdatePositionForPosition(this ProjectileEntity self, float moveDistance)
        {
            if (self.InitData.FlyOver)
            {
                self.CurrentPosition += self.CurrentDirection * moveDistance;
                return;
            }

            if (self.InitData.CurveHeight > 0f && self.TotalDistance > 0.1f)
            {
                self.FlightProgress = Mathf.Clamp01(self.TraveledDistance / self.TotalDistance);
                Vector2 linearPos = Vector2.Lerp(self.StartPosition, self.EndPosition, self.FlightProgress);
                Vector2 forward = (self.EndPosition - self.StartPosition).normalized;
                Vector2 perpendicular = self.GetPerpendicular(forward);
                float curveOffset = self.InitData.CurveHeight * 4f * self.FlightProgress * (1f - self.FlightProgress);
                self.CurrentPosition = linearPos + perpendicular * curveOffset;

                if (self.FlightProgress < 1f)
                {
                    float nextProgress = Mathf.Clamp01((self.TraveledDistance + 0.1f) / self.TotalDistance);
                    Vector2 nextLinearPos = Vector2.Lerp(self.StartPosition, self.EndPosition, nextProgress);
                    float nextCurveOffset = self.InitData.CurveHeight * 4f * nextProgress * (1f - nextProgress);
                    Vector2 nextPos = nextLinearPos + perpendicular * nextCurveOffset;
                    self.CurrentDirection = (nextPos - self.CurrentPosition).normalized;
                }

                return;
            }

            self.CurrentPosition += self.CurrentDirection * moveDistance;
        }

        private static void CheckCollision(this ProjectileEntity self)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.CurrentPosition, self.InitData.CollisionRadius);
            AbilitySystemComponent sourceAsc = self.InitData.SourceASC.As();

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = Collider2DRegistry.GetASC(collider);
                if (asc == null)
                {
                    continue;
                }

                if (self.HitTargetInstanceIds.Contains(asc.InstanceId))
                {
                    continue;
                }

                if (asc == sourceAsc)
                {
                    continue;
                }

                if (!self.IsValidTarget(asc))
                {
                    continue;
                }

                self.HitTargetInstanceIds.Add(asc.InstanceId);
                self.HitCount++;
                self.TriggerHit(asc, self.CurrentPosition);
                if (!self.CanContinue())
                {
                    return;
                }

                if (!self.InitData.IsPiercing)
                {
                    if (self.InitData.IsBouncing)
                    {
                        if (self.BounceCount < self.InitData.MaxBounceCount)
                        {
                            if (self.TryBounceToNextTarget(asc, collider))
                            {
                                return;
                            }

                            return;
                        }

                        self.DestroyProjectile();
                        return;
                    }

                    self.DestroyProjectile();
                    return;
                }

                if (self.HitCount < self.InitData.MaxPierceCount)
                {
                    continue;
                }

                if (self.InitData.IsBouncing && self.BounceCount < self.InitData.MaxBounceCount)
                {
                    if (self.TryBounceToNextTarget(asc, collider))
                    {
                        self.HitCount = 0;
                        return;
                    }

                    self.HitCount = 0;
                    return;
                }

                self.DestroyProjectile();
                return;
            }
        }

        private static void CheckReachTarget(this ProjectileEntity self)
        {
            if (self.InitData.TargetType == ProjectileTargetType.Unit)
            {
                AbilitySystemComponent targetUnit = self.InitData.TargetUnit.As();
                if (targetUnit?.Owner == null && self.InitData.MaxDistance > 0f && self.TraveledDistance >= self.InitData.MaxDistance)
                {
                    self.DestroyProjectile();
                }

                return;
            }

            if (self.InitData.FlyOver)
            {
                if (!self.ReachedTarget)
                {
                    float distToTarget = Vector2.Distance(self.CurrentPosition, self.EndPosition);
                    if (distToTarget < self.InitData.CollisionRadius || self.TraveledDistance >= self.TotalDistance)
                    {
                        self.ReachedTarget = true;
                        self.TriggerReachTarget(self.EndPosition);
                        if (!self.CanContinue())
                        {
                            return;
                        }
                    }
                }

                if (self.InitData.MaxDistance > 0f && self.TraveledDistance >= self.InitData.MaxDistance)
                {
                    self.DestroyProjectile();
                }

                return;
            }

            if (self.FlightProgress >= 1f || Vector2.Distance(self.CurrentPosition, self.EndPosition) < 0.1f)
            {
                self.TriggerReachTarget(self.EndPosition);
                if (!self.CanContinue())
                {
                    return;
                }

                self.DestroyProjectile();
                return;
            }

            if (self.InitData.MaxDistance > 0f && self.TraveledDistance >= self.InitData.MaxDistance)
            {
                self.DestroyProjectile();
            }
        }

        private static bool TryBounceToNextTarget(this ProjectileEntity self, AbilitySystemComponent currentTarget, Collider2D hitCollider)
        {
            self.BounceCount++;

            if (self.InitData.BounceTargetMode == BounceTargetMode.SearchNearest)
            {
                AbilitySystemComponent nextTarget = self.FindNextBounceCandidate(currentTarget);
                if (nextTarget == null)
                {
                    self.BounceCount--;
                    return false;
                }

                if (!self.InitData.CanBounceToSameTarget && currentTarget != null)
                {
                    self.HitTargetInstanceIds.Add(currentTarget.InstanceId);
                }

                self.InitData.TargetUnit = nextTarget;
                self.StartPosition = self.CurrentPosition;
                self.EndPosition = self.GetTargetUnitPosition();
                self.TotalDistance = Vector2.Distance(self.StartPosition, self.EndPosition);
                self.TraveledDistance = 0f;
                self.FlightProgress = 0f;
                self.CurrentDirection = (self.EndPosition - self.StartPosition).normalized;
                self.TriggerBounce(nextTarget, self.CurrentPosition);
                return true;
            }

            Vector2 reflectDirection = self.GetReflectDirection(hitCollider, currentTarget);
            if (Mathf.Abs(self.InitData.BounceAngleOffset) > 0.01f)
            {
                reflectDirection = self.RotateVector2(reflectDirection, self.InitData.BounceAngleOffset);
            }

            if (!self.InitData.CanBounceToSameTarget && currentTarget != null)
            {
                self.HitTargetInstanceIds.Add(currentTarget.InstanceId);
            }

            self.CurrentDirection = reflectDirection;
            self.StartPosition = self.CurrentPosition;
            self.InitData.TargetUnit = default;
            self.TraveledDistance = 0f;
            self.FlightProgress = 0f;
            self.TriggerBounce(null, self.CurrentPosition);
            return true;
        }

        private static AbilitySystemComponent FindNextBounceCandidate(this ProjectileEntity self, AbilitySystemComponent currentTarget)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.CurrentPosition, self.InitData.BounceSearchRadius);
            AbilitySystemComponent nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = Collider2DRegistry.GetASC(collider);
                if (asc == null || asc == self.InitData.SourceASC.As() || asc == currentTarget)
                {
                    continue;
                }

                if (!self.InitData.CanBounceToSameTarget && self.HitTargetInstanceIds.Contains(asc.InstanceId))
                {
                    continue;
                }

                if (self.ShouldExcludeSourceCamp(asc) || !self.IsValidTarget(asc))
                {
                    continue;
                }

                if (asc.Owner == null)
                {
                    continue;
                }

                float distance = Vector2.Distance(self.CurrentPosition, (Vector2)asc.Owner.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = asc;
                }
            }

            return nearestTarget;
        }

        private static void DestroyProjectile(this ProjectileEntity self)
        {
            if (!self.CanContinue())
            {
                return;
            }

            self.Initialized = false;
            self.DestroyRequested = true;

            self.TryTriggerPositionFallbackHit(self.CurrentPosition);

            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            if (effectSpec != null && !effectSpec.IsDisposed)
            {
                effectSpec.RemoveEffect();
            }

            self.Dispose();
        }

        private static bool IsValidTarget(this ProjectileEntity self, AbilitySystemComponent target)
        {
            if (target == null || target.IsDisposed)
            {
                return false;
            }

            if (!self.InitData.CollisionTargetTags.IsEmpty && !target.OwnedTags.HasAnyTags(self.InitData.CollisionTargetTags))
            {
                return false;
            }

            if (!self.InitData.CollisionExcludeTags.IsEmpty && target.OwnedTags.HasAnyTags(self.InitData.CollisionExcludeTags))
            {
                return false;
            }

            return true;
        }

        private static bool ShouldExcludeSourceCamp(this ProjectileEntity self, AbilitySystemComponent target)
        {
            if (!self.InitData.ExcludeSourceCamp)
            {
                return false;
            }

            GameplayTag sourceCampTag = self.GetCampTag(self.InitData.SourceASC.As());
            if (!sourceCampTag.IsValid || target?.OwnedTags == null)
            {
                return false;
            }

            return target.OwnedTags.HasTagExact(sourceCampTag);
        }

        private static GameplayTag GetCampTag(this ProjectileEntity self, AbilitySystemComponent asc)
        {
            if (asc?.OwnedTags == null || asc.OwnedTags.IsEmpty)
            {
                return GameplayTag.None;
            }

            var tags = asc.OwnedTags.Tags;
            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTag tag = tags[i];
                if (tag.IsValid && tag.GetParent() == GameplayTagLibrary.unitType)
                {
                    return tag;
                }
            }

            return GameplayTag.None;
        }

        private static Vector2 GetTargetUnitPosition(this ProjectileEntity self)
        {
            AbilitySystemComponent targetUnit = self.InitData.TargetUnit.As();
            if (targetUnit?.Owner == null)
            {
                return self.EndPosition;
            }

            Transform targetTransform = targetUnit.Owner.transform;
            if (!string.IsNullOrEmpty(self.InitData.TargetBindingName))
            {
                Transform bindingPoint = targetTransform.Find(self.InitData.TargetBindingName);
                if (bindingPoint != null)
                {
                    return bindingPoint.position;
                }
            }

            return targetTransform.position;
        }

        private static Vector2 GetPerpendicular(this ProjectileEntity self, Vector2 direction)
        {
            return new Vector2(direction.y, -direction.x);
        }

        private static Vector2 RotateVector2(this ProjectileEntity self, Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        }

        private static Vector2 GetReflectDirection(this ProjectileEntity self, Collider2D hitCollider, AbilitySystemComponent currentTarget)
        {
            Vector2 surfaceNormal = self.GetSurfaceNormal(hitCollider, currentTarget);
            Vector2 reflectDirection = Vector2.Reflect(self.CurrentDirection.normalized, surfaceNormal).normalized;
            if (reflectDirection.sqrMagnitude <= 0.0001f)
            {
                reflectDirection = -self.CurrentDirection.normalized;
            }

            return reflectDirection;
        }

        private static Vector2 GetSurfaceNormal(this ProjectileEntity self, Collider2D hitCollider, AbilitySystemComponent currentTarget)
        {
            if (hitCollider != null)
            {
                Vector2 closestPoint = hitCollider.ClosestPoint(self.CurrentPosition - (self.CurrentDirection.normalized * self.InitData.CollisionRadius));
                Vector2 surfaceNormal = (self.CurrentPosition - closestPoint).normalized;
                if (surfaceNormal.sqrMagnitude > 0.0001f)
                {
                    return surfaceNormal;
                }
            }

            if (currentTarget?.Owner != null)
            {
                Vector2 fallbackNormal = (self.CurrentPosition - (Vector2)currentTarget.Owner.transform.position).normalized;
                if (fallbackNormal.sqrMagnitude > 0.0001f)
                {
                    return fallbackNormal;
                }
            }

            return -self.CurrentDirection.normalized;
        }

        private static void UpdateRotation(this ProjectileEntity self)
        {
            if (self.CachedTransform == null || self.CurrentDirection.sqrMagnitude <= 0.001f)
            {
                return;
            }

            float angle = Mathf.Atan2(self.CurrentDirection.y, self.CurrentDirection.x) * Mathf.Rad2Deg;
            self.CachedTransform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private static GameplayEffectSpec GetEffectSpec(this ProjectileEntity self)
        {
            if (self == null)
            {
                return null;
            }

            return self.GetParent<GameplayEffectSpec>();
        }

        private static ProjectileEffectSpec GetProjectileSpec(this ProjectileEntity self)
        {
            return self.GetEffectSpec()?.GetComponent<ProjectileEffectSpec>();
        }

        private static GameObject GetProjectileObject(this ProjectileEntity self)
        {
            return self?.CachedTransform != null ? self.CachedTransform.gameObject : null;
        }

        private static void TriggerHit(this ProjectileEntity self, AbilitySystemComponent hitTarget, Vector2 hitPosition)
        {
            if (hitTarget == null)
            {
                return;
            }

            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            ProjectileEffectSpec projectileSpec = self.GetProjectileSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || projectileSpec == null || context == null)
            {
                return;
            }

            projectileSpec.HasTriggeredHit = true;

            SpecExecutionContext hitContext = context.CreateWithParentInput(hitTarget);
            if (hitContext == null)
            {
                return;
            }

            try
            {
                hitContext.SetCustomData("HitPosition", hitPosition);
                hitContext.SetProjectileObject(self.GetProjectileObject());
                hitContext.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, SkillPortId.ProjectileEffect.OnHit);
            }
            finally
            {
                hitContext.Dispose();
            }
        }

        private static void TriggerReachTarget(this ProjectileEntity self, Vector2 position)
        {
            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            ProjectileEffectSpec projectileSpec = self.GetProjectileSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || projectileSpec == null || context == null)
            {
                return;
            }

            self.TryTriggerPositionFallbackHit(position);
            context.SetCustomData("ReachPosition", position);
            context.SetProjectileObject(self.GetProjectileObject());
            context.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, SkillPortId.ProjectileEffect.OnReachTarget);
        }

        private static void TriggerBounce(this ProjectileEntity self, AbilitySystemComponent nextTarget, Vector2 bouncePosition)
        {
            if (nextTarget == null)
            {
                return;
            }

            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || context == null)
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
                bounceContext.SetProjectileObject(self.GetProjectileObject());
                bounceContext.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, SkillPortId.ProjectileEffect.OnBounce);
            }
            finally
            {
                bounceContext.Dispose();
            }
        }

        private static void TryTriggerPositionFallbackHit(this ProjectileEntity self, Vector2 projectilePosition)
        {
            ProjectileEffectSpec projectileSpec = self.GetProjectileSpec();
            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            ProjectileEffectNodeData nodeData = effectSpec?.EffectNodeData as ProjectileEffectNodeData;
            if (projectileSpec == null
                || effectSpec == null
                || nodeData == null
                || projectileSpec.HasTriggeredHit
                || nodeData.projectileTargetType != ProjectileTargetType.Position)
            {
                return;
            }

            float fallbackRadius = Mathf.Max(nodeData.collisionRadius, 0.75f);
            if (Vector2.Distance(projectilePosition, projectileSpec.ExpectedTargetPosition) > fallbackRadius)
            {
                return;
            }

            AbilitySystemComponent fallbackTarget = effectSpec.GetContext()?.GetMainTarget();
            if (fallbackTarget == null)
            {
                return;
            }

            self.TriggerHit(fallbackTarget, projectilePosition);
        }
    }
}
