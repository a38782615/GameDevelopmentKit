using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [TaskHandler]
    [FriendOf(typeof(TaskSpec))]
    public class SearchTargetTaskSpecHandler : ATaskHandler
    {
        public override SpecExecutionContext GetContext()
        {
            return this.Spec?.GetContext();
        }

        public override void Execute()
        {
            SearchTargetTaskNodeData nodeData = this.NodeData as SearchTargetTaskNodeData;
            SpecExecutionContext context = this.GetContext();
            if (nodeData == null || context == null)
            {
                context?.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, SkillPortId.SearchTargetTask.NoTarget);
                return;
            }

            if (!context.GetCaster().IsAlive())
            {
                context.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, SkillPortId.SearchTargetTask.NoTarget);
                return;
            }

            BodyCheckComponent bodyCheckComponent = this.GetBodyCheckComponent();
            List<AbilitySystemComponent> foundTargets = new List<AbilitySystemComponent>();
            float2 centerPosition = ToPlanar(context.GetPosition(nodeData.positionSource, nodeData.positionBindingName));
            GameObject sourceObject = context.GetSourceObject(nodeData.positionSource);
            Transform centerTransform = sourceObject?.transform;

            switch (nodeData.searchShapeType)
            {
                case SearchShapeType.Circle:
                    this.SearchCircle(foundTargets, bodyCheckComponent, centerPosition, nodeData.searchCircleRadius);
                    this.DebugDrawCircle(centerPosition, nodeData.searchCircleRadius);
                    break;
                case SearchShapeType.Sector:
                    if (centerTransform != null)
                    {
                        float2 sectorForward = this.GetFacingDirection(centerTransform);
                        this.SearchSector(foundTargets, bodyCheckComponent, centerPosition, sectorForward, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                        this.DebugDrawSector(centerPosition, sectorForward, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                    }
                    break;
                case SearchShapeType.Line:
                    this.SearchLine(foundTargets, bodyCheckComponent, centerPosition, centerTransform);
                    break;
            }

            if (nodeData.maxTargets > 0 && foundTargets.Count > nodeData.maxTargets)
            {
                foundTargets.RemoveRange(nodeData.maxTargets, foundTargets.Count - nodeData.maxTargets);
            }

            if (foundTargets.Count == 0)
            {
                context.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, SkillPortId.SearchTargetTask.NoTarget);
            }
            else
            {
                foreach (AbilitySystemComponent findTarget in foundTargets)
                {
                    SpecExecutionContext targetContext = context.CreateWithParentInput(findTarget);
                    if (targetContext == null)
                    {
                        continue;
                    }

                    try
                    {
                        targetContext.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, SkillPortId.SearchTargetTask.ForEachTarget);
                    }
                    finally
                    {
                        targetContext.Dispose();
                    }
                }
            }

            context.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, SkillPortId.SearchTargetTask.Complete);
        }

        private void SearchCircle(List<AbilitySystemComponent> foundTargets, BodyCheckComponent bodyCheckComponent, float2 center, float radius)
        {
            if (bodyCheckComponent == null)
            {
                return;
            }

            List<EntityRef<EntityBody>> bodies = new List<EntityRef<EntityBody>>();
            bodyCheckComponent.SearchCircle(center, radius, bodies);
            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent asc = bodyRef.As()?.GetAbilitySystem();
                if (asc != null && this.IsValidTarget(asc))
                {
                    foundTargets.Add(asc);
                }
            }
        }

        private void SearchSector(List<AbilitySystemComponent> foundTargets, BodyCheckComponent bodyCheckComponent, float2 center, float2 forward, float radius, float angle)
        {
            if (bodyCheckComponent == null)
            {
                return;
            }

            List<EntityRef<EntityBody>> bodies = new List<EntityRef<EntityBody>>();
            bodyCheckComponent.SearchSector(center, forward, radius, angle, bodies);
            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent asc = bodyRef.As()?.GetAbilitySystem();
                if (asc != null && this.IsValidTarget(asc))
                {
                    foundTargets.Add(asc);
                }
            }
        }

        private void SearchLine(List<AbilitySystemComponent> foundTargets, BodyCheckComponent bodyCheckComponent, float2 center, Transform casterTransform)
        {
            SearchTargetTaskNodeData nodeData = this.NodeData as SearchTargetTaskNodeData;
            if (nodeData == null || bodyCheckComponent == null)
            {
                return;
            }

            float2 direction;
            float width;
            float length;
            float2 startPos = center;
            float2 baseForward = casterTransform != null ? this.GetFacingDirection(casterTransform) : new float2(1f, 0f);
            SpecExecutionContext context = this.GetContext();

            switch (nodeData.searchLineType)
            {
                case SkillLineType.UnitDirection:
                    direction = this.RotateVector2(baseForward, nodeData.searchLineDirectionOffsetAngle);
                    width = nodeData.searchLineDirectionWidth;
                    length = nodeData.searchLineDirectionLength;
                    break;
                case SkillLineType.BetweenPoints:
                    startPos = ToPlanar(context.GetPosition(nodeData.lineStartPositionSource, nodeData.lineStartBindingName));
                    float2 endPos = ToPlanar(context.GetPosition(nodeData.lineEndPositionSource, nodeData.lineEndBindingName));
                    direction = math.normalizesafe(endPos - startPos, new float2(1f, 0f));
                    width = nodeData.searchLineBetweenWidth;
                    length = math.distance(startPos, endPos);
                    break;
                default:
                    direction = this.RotateVector2(new float2(1f, 0f), nodeData.searchLineAbsoluteAngle);
                    width = nodeData.searchLineAbsoluteWidth;
                    length = nodeData.searchLineAbsoluteLength;
                    break;
            }

            float2 boxCenter = startPos + direction * (length * 0.5f);
            float2 boxSize = new float2(length, width);
            float boxAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            this.DebugDrawBox(boxCenter, boxSize, boxAngle);

            List<EntityRef<EntityBody>> bodies = new List<EntityRef<EntityBody>>();
            bodyCheckComponent.SearchBox(boxCenter, boxSize, boxAngle, bodies);
            foreach (EntityRef<EntityBody> bodyRef in bodies)
            {
                AbilitySystemComponent asc = bodyRef.As()?.GetAbilitySystem();
                if (asc != null && this.IsValidTarget(asc))
                {
                    foundTargets.Add(asc);
                }
            }
        }

        private BodyCheckComponent GetBodyCheckComponent()
        {
            Unit unit = this.GetContext()?.GetCaster()?.GetParent<SkillUnit>()?.Unit.As();
            return unit?.Scene()?.GetComponent<BodyCheckComponent>();
        }

        private float2 RotateVector2(float2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new float2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private float2 GetFacingDirection(Transform casterTransform)
        {
            return casterTransform.localScale.x >= 0 ? new float2(-1f, 0f) : new float2(1f, 0f);
        }

        private bool IsValidTarget(AbilitySystemComponent target)
        {
            if (target == null)
            {
                return false;
            }

            if (!target.IsAlive())
            {
                return false;
            }

            AbilitySystemComponent currentTarget = this.Spec.GetTaskTarget();
            if (target == currentTarget)
            {
                return false;
            }

            SearchTargetTaskNodeData nodeData = this.NodeData as SearchTargetTaskNodeData;
            if (nodeData == null)
            {
                return false;
            }

            if (!nodeData.searchTargetTags.IsEmpty && !target.HasAnyTags(nodeData.searchTargetTags))
            {
                return false;
            }

            if (!nodeData.searchExcludeTags.IsEmpty && target.HasAnyTags(nodeData.searchExcludeTags))
            {
                return false;
            }

            return true;
        }

        private void DebugDrawCircle(float2 center, float radius)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
            {
                return;
            }

            const int segments = 32;
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                Vector3 p1 = new Vector3(center.x + Mathf.Cos(angle1) * radius, center.y + Mathf.Sin(angle1) * radius, 0f);
                Vector3 p2 = new Vector3(center.x + Mathf.Cos(angle2) * radius, center.y + Mathf.Sin(angle2) * radius, 0f);
                Debug.DrawLine(p1, p2, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            }
        }

        private void DebugDrawSector(float2 center, float2 forward, float radius, float angle)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
            {
                return;
            }

            float halfAngle = angle * 0.5f;
            int arcSegments = Mathf.Max(8, (int)(angle / 10f));
            float angleStep = angle / arcSegments;
            float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float startAngle = forwardAngle - halfAngle;
            Vector3 center3D = new Vector3(center.x, center.y, 0f);

            Vector3 leftEdge = center3D + new Vector3(
                Mathf.Cos((forwardAngle - halfAngle) * Mathf.Deg2Rad) * radius,
                Mathf.Sin((forwardAngle - halfAngle) * Mathf.Deg2Rad) * radius,
                0f);
            Vector3 rightEdge = center3D + new Vector3(
                Mathf.Cos((forwardAngle + halfAngle) * Mathf.Deg2Rad) * radius,
                Mathf.Sin((forwardAngle + halfAngle) * Mathf.Deg2Rad) * radius,
                0f);

            Debug.DrawLine(center3D, leftEdge, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(center3D, rightEdge, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);

            for (int i = 0; i < arcSegments; i++)
            {
                float angle1 = (startAngle + i * angleStep) * Mathf.Deg2Rad;
                float angle2 = (startAngle + (i + 1) * angleStep) * Mathf.Deg2Rad;
                Vector3 p1 = center3D + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0f);
                Vector3 p2 = center3D + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0f);
                Debug.DrawLine(p1, p2, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            }
        }

        private void DebugDrawBox(float2 center, float2 size, float angle)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
            {
                return;
            }

            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            float2[] localCorners =
            {
                new float2(-halfWidth, -halfHeight),
                new float2(halfWidth, -halfHeight),
                new float2(halfWidth, halfHeight),
                new float2(-halfWidth, halfHeight)
            };

            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float rotatedX = localCorners[i].x * cos - localCorners[i].y * sin;
                float rotatedY = localCorners[i].x * sin + localCorners[i].y * cos;
                worldCorners[i] = new Vector3(center.x + rotatedX, center.y + rotatedY, 0f);
            }

            Debug.DrawLine(worldCorners[0], worldCorners[1], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[1], worldCorners[2], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[2], worldCorners[3], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[3], worldCorners[0], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
        }

        private static float2 ToPlanar(Vector3 value)
        {
            return global::ET.ModeDefine.Is2D ? new float2(value.x, value.y) : new float2(value.x, value.z);
        }
    }
}
