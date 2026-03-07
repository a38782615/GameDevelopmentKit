

using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(ProjectileRuntimeComponent))]
    [FriendOf(typeof(ProjectileRuntimeComponent))]
    [FriendOf(typeof(ProjectileEffectSpec))]
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public static partial class ProjectileRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ProjectileRuntimeComponent self, ProjectileInitData data)
        {
            self.Data = data;
            self.IsInitialized = true;
            self.ReachedTarget = false;
            self.CurrentPosition = data.LaunchPosition;
            self.CurrentDirection = data.Direction;
            self.StartPosition = data.LaunchPosition;
            self.EndPosition = data.TargetPosition;
            self.TotalDistance = Vector2.Distance(self.StartPosition, self.EndPosition);
            self.TraveledDistance = 0f;
            self.FlightProgress = 0f;
            self.HitCount = 0;
            self.BounceCount = 0;
            self.HitTargetIds.Clear();

            self.UpdateProjectileTransform();
        }

        [EntitySystem]
        private static void Destroy(this ProjectileRuntimeComponent self)
        {
            self.IsInitialized = false;
            self.HitTargetIds?.Clear();
        }

        public static void TickRuntime(this ProjectileRuntimeComponent self, float deltaTime)
        {
            if (!self.IsInitialized || deltaTime <= 0f)
            {
                return;
            }

            self.UpdatePosition(deltaTime);
            self.CheckCollision();

            if (!self.IsInitialized || self.IsDisposed)
            {
                return;
            }

            self.CheckReachTarget();

            if (!self.IsInitialized || self.IsDisposed)
            {
                return;
            }

            self.UpdateProjectileTransform();
        }

        public static void CancelRuntime(this ProjectileRuntimeComponent self)
        {
            self.CleanupProjectile(true);
        }

        private static void UpdatePosition(this ProjectileRuntimeComponent self, float deltaTime)
        {
            float moveDistance = self.Data.Speed * deltaTime;
            self.TraveledDistance += moveDistance;

            if (self.Data.TargetType == ProjectileTargetType.Unit)
            {
                self.UpdatePositionForUnit(moveDistance);
                return;
            }

            self.UpdatePositionForPosition(moveDistance);
        }

        private static void UpdatePositionForUnit(this ProjectileRuntimeComponent self, float moveDistance)
        {
            var targetUnit = self.Data.TargetUnit.As();
            if (targetUnit?.Owner != null)
            {
                self.EndPosition = self.GetTargetUnitPosition();
                self.TotalDistance = Vector2.Distance(self.CurrentPosition, self.EndPosition);
            }

            if (self.Data.CurveHeight > 0f && self.TotalDistance > 0.1f)
            {
                self.FlightProgress = Mathf.Clamp01(self.TraveledDistance / (self.TotalDistance + self.TraveledDistance));

                Vector2 linearPos = Vector2.MoveTowards(self.CurrentPosition, self.EndPosition, moveDistance);
                float curveOffset = self.Data.CurveHeight * 4f * self.FlightProgress * (1f - self.FlightProgress);
                Vector2 perpendicular = self.GetPerpendicular(self.CurrentDirection);

                self.CurrentPosition = linearPos + perpendicular * curveOffset;
                self.CurrentDirection = (self.EndPosition - self.CurrentPosition).normalized;
                return;
            }

            self.CurrentDirection = (self.EndPosition - self.CurrentPosition).normalized;
            self.CurrentPosition = Vector2.MoveTowards(self.CurrentPosition, self.EndPosition, moveDistance);
        }

        private static void UpdatePositionForPosition(this ProjectileRuntimeComponent self, float moveDistance)
        {
            if (self.Data.FlyOver)
            {
                self.CurrentPosition += self.CurrentDirection * moveDistance;
                return;
            }

            if (self.Data.CurveHeight > 0f && self.TotalDistance > 0.1f)
            {
                self.FlightProgress = Mathf.Clamp01(self.TraveledDistance / self.TotalDistance);

                Vector2 linearPos = Vector2.Lerp(self.StartPosition, self.EndPosition, self.FlightProgress);
                Vector2 perpendicular = self.GetPerpendicular((self.EndPosition - self.StartPosition).normalized);
                float curveOffset = self.Data.CurveHeight * 4f * self.FlightProgress * (1f - self.FlightProgress);

                self.CurrentPosition = linearPos + perpendicular * curveOffset;

                if (self.FlightProgress < 1f)
                {
                    float nextProgress = Mathf.Clamp01((self.TraveledDistance + 0.1f) / self.TotalDistance);
                    Vector2 nextLinearPos = Vector2.Lerp(self.StartPosition, self.EndPosition, nextProgress);
                    float nextCurveOffset = self.Data.CurveHeight * 4f * nextProgress * (1f - nextProgress);
                    Vector2 nextPos = nextLinearPos + perpendicular * nextCurveOffset;
                    self.CurrentDirection = (nextPos - self.CurrentPosition).normalized;
                }

                return;
            }

            self.CurrentPosition += self.CurrentDirection * moveDistance;
        }

        private static Vector2 GetTargetUnitPosition(this ProjectileRuntimeComponent self)
        {
            var targetUnit = self.Data.TargetUnit.As();
            if (targetUnit?.Owner == null)
            {
                return self.EndPosition;
            }

            Transform transform = targetUnit.Owner.transform;
            if (!string.IsNullOrEmpty(self.Data.TargetBindingName))
            {
                Transform bindingPoint = transform.Find(self.Data.TargetBindingName);
                if (bindingPoint != null)
                {
                    return bindingPoint.position;
                }
            }

            return transform.position;
        }

        private static void CheckCollision(this ProjectileRuntimeComponent self)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.CurrentPosition, self.Data.CollisionRadius);

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = self.GetASCFromCollider(collider);
                if (asc == null)
                {
                    continue;
                }

                if (self.HitTargetIds.Contains(asc.Id))
                {
                    continue;
                }

                if (asc == self.Data.SourceASC.As())
                {
                    continue;
                }

                if (!self.IsValidTarget(asc))
                {
                    continue;
                }

                self.HitTargetIds.Add(asc.Id);
                self.HitCount++;
                self.TriggerHit(asc, self.CurrentPosition);

                if (!self.Data.IsPiercing)
                {
                    if (self.Data.IsBouncing)
                    {
                        if (self.BounceCount < self.Data.MaxBounceCount)
                        {
                            if (self.TryBounceToNextTarget(asc))
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

                if (self.HitCount >= self.Data.MaxPierceCount)
                {
                    if (self.Data.IsBouncing && self.BounceCount < self.Data.MaxBounceCount)
                    {
                        if (self.TryBounceToNextTarget(asc))
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
        }

        private static void CheckReachTarget(this ProjectileRuntimeComponent self)
        {
            if (self.Data.TargetType == ProjectileTargetType.Unit)
            {
                if (self.Data.TargetUnit.As()?.Owner == null && self.Data.MaxDistance > 0f && self.TraveledDistance >= self.Data.MaxDistance)
                {
                    self.DestroyProjectile();
                }

                return;
            }

            if (self.Data.FlyOver)
            {
                if (!self.ReachedTarget)
                {
                    float distToTarget = Vector2.Distance(self.CurrentPosition, self.EndPosition);
                    if (distToTarget < self.Data.CollisionRadius || self.TraveledDistance >= self.TotalDistance)
                    {
                        self.ReachedTarget = true;
                        self.TriggerReachTarget(self.EndPosition);
                    }
                }

                if (self.Data.MaxDistance > 0f && self.TraveledDistance >= self.Data.MaxDistance)
                {
                    self.DestroyProjectile();
                }

                return;
            }

            if (self.FlightProgress >= 1f || Vector2.Distance(self.CurrentPosition, self.EndPosition) < 0.1f)
            {
                self.TriggerReachTarget(self.EndPosition);
                self.DestroyProjectile();
                return;
            }

            if (self.Data.MaxDistance > 0f && self.TraveledDistance >= self.Data.MaxDistance)
            {
                self.DestroyProjectile();
            }
        }

        private static AbilitySystemComponent GetASCFromCollider(this ProjectileRuntimeComponent self, Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            SkillUnit unit = collider.GetComponent<SkillUnit>();
            if (unit != null)
            {
                return unit.ASC.As();
            }

            unit = collider.GetComponentInParent<SkillUnit>();
            if (unit != null)
            {
                return unit.ASC.As();
            }

            return null;
        }

        private static bool IsValidTarget(this ProjectileRuntimeComponent self, AbilitySystemComponent target)
        {
            if (target == null)
            {
                return false;
            }

            if (!self.Data.CollisionTargetTags.IsEmpty && !target.HasAnyTags(self.Data.CollisionTargetTags))
            {
                return false;
            }

            if (!self.Data.CollisionExcludeTags.IsEmpty && target.HasAnyTags(self.Data.CollisionExcludeTags))
            {
                return false;
            }

            return true;
        }

        private static bool TryBounceToNextTarget(this ProjectileRuntimeComponent self, AbilitySystemComponent currentTarget)
        {
            self.BounceCount++;

            if (self.Data.BounceTargetMode == BounceTargetMode.SearchNearest)
            {
                AbilitySystemComponent nextTarget = self.FindNextBounceTarget(currentTarget);
                if (nextTarget == null)
                {
                    self.BounceCount--;
                    return false;
                }

                if (!self.Data.CanBounceToSameTarget && currentTarget != null)
                {
                    self.HitTargetIds.Add(currentTarget.Id);
                }

                self.Data.TargetUnit = nextTarget;
                self.StartPosition = self.CurrentPosition;
                self.EndPosition = self.GetTargetUnitPosition();
                self.TotalDistance = Vector2.Distance(self.StartPosition, self.EndPosition);
                self.TraveledDistance = 0f;
                self.FlightProgress = 0f;
                self.CurrentDirection = (self.EndPosition - self.StartPosition).normalized;

                self.TriggerBounce(nextTarget, self.CurrentPosition);
                return true;
            }

            Vector2 reverseDirection = -self.CurrentDirection;
            if (Mathf.Abs(self.Data.BounceAngleOffset) > 0.01f)
            {
                reverseDirection = self.RotateVector2(reverseDirection, self.Data.BounceAngleOffset);
            }

            if (!self.Data.CanBounceToSameTarget && currentTarget != null)
            {
                self.HitTargetIds.Add(currentTarget.Id);
            }

            self.CurrentDirection = reverseDirection;
            self.StartPosition = self.CurrentPosition;
            self.Data.TargetUnit = default;
            self.TraveledDistance = 0f;
            self.FlightProgress = 0f;

            self.TriggerBounce(null, self.CurrentPosition);
            return true;
        }

        private static AbilitySystemComponent FindNextBounceTarget(this ProjectileRuntimeComponent self, AbilitySystemComponent currentTarget)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.CurrentPosition, self.Data.BounceSearchRadius);

            AbilitySystemComponent nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = self.GetASCFromCollider(collider);
                if (asc == null)
                {
                    continue;
                }

                if (asc == self.Data.SourceASC.As() || asc == currentTarget)
                {
                    continue;
                }

                if (!self.Data.CanBounceToSameTarget && self.HitTargetIds.Contains(asc.Id))
                {
                    continue;
                }

                if (!self.IsValidTarget(asc) || asc.Owner == null)
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

        private static void TriggerHit(this ProjectileRuntimeComponent self, AbilitySystemComponent hitTarget, Vector2 hitPosition)
        {
            if (hitTarget == null)
            {
                return;
            }

            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || context == null)
            {
                return;
            }

            SpecExecutionContext hitContext = context.CreateWithParentInput(hitTarget);
            hitContext.SetCustomData("HitPosition", hitPosition);
            hitContext.ProjectileObject = self.GetProjectileObject();
            hitContext.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, "碰撞时");
        }

        private static void TriggerReachTarget(this ProjectileRuntimeComponent self, Vector2 position)
        {
            GameplayEffectSpec effectSpec = self.GetEffectSpec();
            SpecExecutionContext context = effectSpec?.GetContext();
            if (effectSpec == null || context == null)
            {
                return;
            }

            context.SetCustomData("ReachPosition", position);
            context.ProjectileObject = self.GetProjectileObject();
            context.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, "到达目标位置");
        }

        private static void TriggerBounce(this ProjectileRuntimeComponent self, AbilitySystemComponent nextTarget, Vector2 bouncePosition)
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
            bounceContext.SetCustomData("BouncePosition", bouncePosition);
            bounceContext.ProjectileObject = self.GetProjectileObject();
            bounceContext.ExecuteConnectedNodes(effectSpec.SkillId, effectSpec.NodeGuid, "反弹时");
        }

        private static void DestroyProjectile(this ProjectileRuntimeComponent self)
        {
            self.CleanupProjectile(false);
        }

        private static void CleanupProjectile(this ProjectileRuntimeComponent self, bool isCancel)
        {
            if (self.IsDisposed)
            {
                return;
            }

            self.IsInitialized = false;

            ProjectileEffectSpec selfSpec = self.GetSelfSpec();
            GameplayEffectSpec effectSpec = selfSpec?.GetParent<GameplayEffectSpec>();
            GameObject projectileObject = selfSpec?._projectileObject;
            if (selfSpec != null)
            {
                selfSpec._projectileRuntime = default;
                selfSpec._projectileObject = null;
            }

            if (projectileObject != null)
            {
                ProjectileController controller = projectileObject.GetComponent<ProjectileController>();
                if (controller != null)
                {
                    controller.Runtime = default;
                }

                UnityEngine.Object.Destroy(projectileObject);
            }

            self.Dispose();

            if (effectSpec == null || effectSpec.IsDisposed)
            {
                return;
            }

            if (isCancel)
            {
                effectSpec.CancelEffect();
                return;
            }

            effectSpec.RemoveEffect();
        }

        private static void UpdateProjectileTransform(this ProjectileRuntimeComponent self)
        {
            GameObject projectileObject = self.GetProjectileObject();
            if (projectileObject == null)
            {
                return;
            }

            projectileObject.transform.position = self.CurrentPosition;
            if (self.CurrentDirection.sqrMagnitude > 0.001f)
            {
                float angle = Mathf.Atan2(self.CurrentDirection.y, self.CurrentDirection.x) * Mathf.Rad2Deg;
                projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }

        private static Vector2 RotateVector2(this ProjectileRuntimeComponent self, Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private static Vector2 GetPerpendicular(this ProjectileRuntimeComponent self, Vector2 dir)
        {
            return new Vector2(dir.y, -dir.x);
        }

        private static ProjectileEffectSpec GetSelfSpec(this ProjectileRuntimeComponent self)
        {
            return self.GetParent<ProjectileEffectSpec>();
        }

        private static GameplayEffectSpec GetEffectSpec(this ProjectileRuntimeComponent self)
        {
            ProjectileEffectSpec selfSpec = self.GetSelfSpec();
            return selfSpec?.GetParent<GameplayEffectSpec>();
        }

        private static GameObject GetProjectileObject(this ProjectileRuntimeComponent self)
        {
            return self.GetSelfSpec()?._projectileObject;
        }
    }
}