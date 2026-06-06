using System.Collections.Generic;
using Unity.Mathematics;

namespace ET.Client
{
    [EntitySystemOf(typeof(BodyCheckComponent))]
    [FriendOf(typeof(BodyCheckComponent))]
    [FriendOf(typeof(EntityBody))]
    public static partial class BodyCheckComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BodyCheckComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this BodyCheckComponent self)
        {
            self.Clear();
        }

        public static void Register(this BodyCheckComponent self, EntityBody body)
        {
            if (self == null || body == null || body.IsDisposed)
            {
                return;
            }

            self.Bodies[body.Id] = body;
            self.IsTreeDirty = true;
        }

        public static void Unregister(this BodyCheckComponent self, EntityBody body)
        {
            if (self == null || body == null)
            {
                return;
            }

            self.Bodies.Remove(body.Id);
            self.IsTreeDirty = true;
        }

        public static void MarkDirty(this BodyCheckComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.IsTreeDirty = true;
        }

        public static void MarkDirty(this BodyCheckComponent self, EntityBody body)
        {
            if (self == null || body == null || body.IsDisposed)
            {
                return;
            }

            self.IsTreeDirty = true;
        }

        public static void SearchCircle(this BodyCheckComponent self, float2 center, float radius, List<EntityRef<EntityBody>> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (self == null)
            {
                return;
            }

            self.CollectCandidates(center, radius + self.MaxBoundingRadius);
            foreach (int candidateIndex in self.CandidateIndices)
            {
                EntityBody body = self.IndexedBodies[candidateIndex].As();
                if (body != null && self.OverlapCircle(center, radius, body))
                {
                    results.Add(body);
                }
            }
        }

        public static void SearchSector(this BodyCheckComponent self, float2 center, float2 forward, float radius, float angleDeg, List<EntityRef<EntityBody>> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (self == null)
            {
                return;
            }

            if (math.lengthsq(forward) < 0.0001f)
            {
                return;
            }

            float2 normalizedForward = math.normalize(forward);
            float halfAngle = angleDeg * 0.5f;
            self.CollectCandidates(center, radius + self.MaxBoundingRadius);
            foreach (int candidateIndex in self.CandidateIndices)
            {
                EntityBody body = self.IndexedBodies[candidateIndex].As();
                if (body == null || !self.OverlapCircle(center, radius, body))
                {
                    continue;
                }

                float2 toTarget = GetBodyCenter(body) - center;
                if (math.lengthsq(toTarget) < 0.0001f)
                {
                    results.Add(body);
                    continue;
                }

                float dot = math.dot(normalizedForward, math.normalize(toTarget));
                dot = math.clamp(dot, -1f, 1f);
                float angle = math.degrees(math.acos(dot));
                if (angle <= halfAngle)
                {
                    results.Add(body);
                }
            }
        }

        public static void SearchBox(this BodyCheckComponent self, float2 center, float2 size, float angleDeg, List<EntityRef<EntityBody>> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            if (self == null)
            {
                return;
            }

            float queryRadius = math.length(size * 0.5f) + self.MaxBoundingRadius;
            self.CollectCandidates(center, queryRadius);
            foreach (int candidateIndex in self.CandidateIndices)
            {
                EntityBody body = self.IndexedBodies[candidateIndex].As();
                if (body != null && self.OverlapBox(center, size, angleDeg, body))
                {
                    results.Add(body);
                }
            }
        }

        public static EntityBody SearchNearest(this BodyCheckComponent self, float2 center, float radius)
        {
            if (self == null)
            {
                return null;
            }

            self.CollectCandidates(center, radius + self.MaxBoundingRadius);
            EntityBody nearest = null;
            float nearestDistanceSq = float.MaxValue;
            foreach (int candidateIndex in self.CandidateIndices)
            {
                EntityBody body = self.IndexedBodies[candidateIndex].As();
                if (body == null || !self.OverlapCircle(center, radius, body))
                {
                    continue;
                }

                float distanceSq = math.distancesq(center, GetBodyCenter(body));
                if (distanceSq < nearestDistanceSq)
                {
                    nearestDistanceSq = distanceSq;
                    nearest = body;
                }
            }

            return nearest;
        }

        public static bool OverlapCircle(this BodyCheckComponent self, float2 center, float radius, EntityBody target)
        {
            if (self == null || target == null || target.IsDisposed)
            {
                return false;
            }

            float2 targetCenter = GetBodyCenter(target);
            if (IsCircle(target))
            {
                float combinedRadius = radius + target.Width * 0.5f;
                return math.distancesq(center, targetCenter) <= combinedRadius * combinedRadius;
            }

            float2 halfExtents = new float2(target.Width * 0.5f, target.Height * 0.5f);
            float2 offset = center - targetCenter;
            float2 clamped = math.clamp(offset, -halfExtents, halfExtents);
            float2 nearestPoint = targetCenter + clamped;
            return math.distancesq(center, nearestPoint) <= radius * radius;
        }

        public static bool OverlapBox(this BodyCheckComponent self, float2 center, float2 size, float angleDeg, EntityBody target)
        {
            if (self == null || target == null || target.IsDisposed)
            {
                return false;
            }

            float2 queryHalfExtents = size * 0.5f;
            float radians = math.radians(angleDeg);
            float cos = math.cos(radians);
            float sin = math.sin(radians);
            float2 axisX = new float2(cos, sin);
            float2 axisY = new float2(-sin, cos);
            float2 targetCenter = GetBodyCenter(target);

            if (IsCircle(target))
            {
                float2 delta = targetCenter - center;
                float2 local = new float2(math.dot(delta, axisX), math.dot(delta, axisY));
                float2 clamped = math.clamp(local, -queryHalfExtents, queryHalfExtents);
                float2 closest = center + axisX * clamped.x + axisY * clamped.y;
                float radius = target.Width * 0.5f;
                return math.distancesq(targetCenter, closest) <= radius * radius;
            }

            float2 targetHalfExtents = new float2(target.Width * 0.5f, target.Height * 0.5f);
            float2 translation = targetCenter - center;

            if (!OverlapOnAxis(translation, queryHalfExtents, targetHalfExtents, new float2(1f, 0f), axisX, axisY))
            {
                return false;
            }

            if (!OverlapOnAxis(translation, queryHalfExtents, targetHalfExtents, new float2(0f, 1f), axisX, axisY))
            {
                return false;
            }

            if (!OverlapOnAxis(translation, queryHalfExtents, targetHalfExtents, axisX, axisX, axisY))
            {
                return false;
            }

            return OverlapOnAxis(translation, queryHalfExtents, targetHalfExtents, axisY, axisX, axisY);
        }

        public static void Clear(this BodyCheckComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.Bodies.Clear();
            self.IndexedBodies.Clear();
            self.IndexedPoints.Clear();
            self.CandidateIndices.Clear();
            self.MaxBoundingRadius = 0f;
            self.IsTreeDirty = true;
        }

        private static void CollectCandidates(this BodyCheckComponent self, float2 center, float radius)
        {
            self.CandidateIndices.Clear();
            self.EnsureTreeBuilt();
            if (self.IndexedBodies.Count == 0)
            {
                return;
            }

            self.KDQuery.Radius(self.KDTree, ToKDPoint(center), radius, self.CandidateIndices);
        }

        private static void EnsureTreeBuilt(this BodyCheckComponent self)
        {
            if (self == null || !self.IsTreeDirty)
            {
                return;
            }

            self.RebuildTree();
        }

        private static void RebuildTree(this BodyCheckComponent self)
        {
            self.IndexedBodies.Clear();
            self.IndexedPoints.Clear();
            self.MaxBoundingRadius = 0f;

            foreach (EntityRef<EntityBody> bodyRef in self.Bodies.Values)
            {
                EntityBody body = bodyRef.As();
                if (body == null || body.IsDisposed)
                {
                    continue;
                }

                self.IndexedBodies.Add(body);
                self.IndexedPoints.Add(ToKDPoint(GetBodyCenter(body)));
                self.MaxBoundingRadius = math.max(self.MaxBoundingRadius, GetBoundingRadius(body));
            }

            if (self.IndexedPoints.Count > 0)
            {
                self.KDTree.Build(self.IndexedPoints);
            }
            else
            {
                self.KDTree.SetCount(0);
            }

            self.IsTreeDirty = false;
        }

        private static float3 ToKDPoint(float2 value)
        {
            return new float3(value.x, value.y, 0f);
        }

        private static float2 GetBodyCenter(EntityBody body)
        {
            Unit unit = body?.GetParent<Unit>();
            return unit == null ? float2.zero : unit.Position.ToPlanar();
        }

        private static float GetBoundingRadius(EntityBody body)
        {
            if (body == null)
            {
                return 0f;
            }

            if (IsCircle(body))
            {
                return body.Width * 0.5f;
            }

            return math.length(new float2(body.Width, body.Height)) * 0.5f;
        }

        private static bool IsCircle(EntityBody body)
        {
            return body != null && body.Shape == EntityBody.ShapeType.CircleShape;
        }

        private static bool OverlapOnAxis(float2 translation, float2 queryHalfExtents, float2 targetHalfExtents, float2 axis, float2 queryAxisX, float2 queryAxisY)
        {
            float distance = math.abs(math.dot(translation, axis));
            float queryProjection = queryHalfExtents.x * math.abs(math.dot(queryAxisX, axis))
                + queryHalfExtents.y * math.abs(math.dot(queryAxisY, axis));
            float targetProjection = targetHalfExtents.x * math.abs(axis.x) + targetHalfExtents.y * math.abs(axis.y);
            return distance <= queryProjection + targetProjection;
        }
    }
}
