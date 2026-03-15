using System.Collections.Generic;
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
            var nodeData = this.NodeData as SearchTargetTaskNodeData;
            var context = this.GetContext();
            if (nodeData == null || context == null)
            {
                context?.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, "无目标");
                return;
            }

            List<AbilitySystemComponent> foundTargets = new();
            Vector2 centerPosition = context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);
            GameObject sourceObject = context.GetSourceObject(nodeData.positionSource);
            Transform centerTransform = sourceObject?.transform;

            switch (nodeData.searchShapeType)
            {
                case SearchShapeType.Circle:
                    this.SearchCircle(foundTargets, centerPosition, nodeData.searchCircleRadius);
                    this.DebugDrawCircle(centerPosition, nodeData.searchCircleRadius);
                    break;
                case SearchShapeType.Sector:
                    if (centerTransform != null)
                    {
                        Vector2 sectorForward = this.GetFacingDirection(centerTransform);
                        this.SearchSector(foundTargets, centerPosition, centerTransform, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                        this.DebugDrawSector(centerPosition, sectorForward, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                    }
                    break;
                case SearchShapeType.Line:
                    this.SearchLine(foundTargets, centerPosition, centerTransform);
                    break;
            }

            if (nodeData.maxTargets > 0 && foundTargets.Count > nodeData.maxTargets)
                foundTargets.RemoveRange(nodeData.maxTargets, foundTargets.Count - nodeData.maxTargets);

            if (foundTargets.Count == 0)
            {
                context.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, "无目标");
            }
            else
            {
                foreach (AbilitySystemComponent findTarget in foundTargets)
                {
                    SpecExecutionContext targetContext = context.CreateWithParentInput(findTarget);
                    targetContext.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, "对每个目标");
                }
            }

            context.ExecuteConnectedNodes(this.Spec.SkillId, this.Spec.NodeGuid, "完成效果");
        }

        private void SearchCircle(List<AbilitySystemComponent> foundTargets, Vector2 center, float radius)
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = this.GetASCFromCollider(collider);
                if (asc != null && this.IsValidTarget(asc))
                    foundTargets.Add(asc);
            }
        }

        private void SearchSector(List<AbilitySystemComponent> foundTargets, Vector2 center, Transform casterTransform, float radius, float angle)
        {
            float halfAngle = angle * 0.5f;
            Vector2 forward = this.GetFacingDirection(casterTransform);
            bool enableDiag = this.ShouldLogSkill1001();

            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);
            if (enableDiag)
            {
            }

            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = this.GetASCFromCollider(collider);
                bool isValidTarget = asc != null && this.IsValidTarget(asc);
                if (asc == null || !isValidTarget)
                {
                    if (enableDiag)
                    {
                    }
                    continue;
                }

                Vector2 toTarget = (Vector2)collider.transform.position - center;
                if (toTarget.sqrMagnitude < 0.001f)
                {
                    if (enableDiag)
                    {
                    }
                    continue;
                }

                float angleToTarget = Vector2.Angle(forward, toTarget);
                if (enableDiag)
                {
                }

                if (angleToTarget <= halfAngle)
                {
                    foundTargets.Add(asc);
                    if (enableDiag)
                    {
                    }
                }
            }

            if (enableDiag)
            {
            }
        }

        private void SearchLine(List<AbilitySystemComponent> foundTargets, Vector2 center, Transform casterTransform)
        {
            var nodeData = this.NodeData as SearchTargetTaskNodeData;
            if (nodeData == null)
                return;

            Vector2 direction;
            float width;
            float length;
            Vector2 startPos = center;
            Vector2 baseForward = casterTransform != null ? this.GetFacingDirection(casterTransform) : Vector2.right;
            SpecExecutionContext context = this.GetContext();

            switch (nodeData.searchLineType)
            {
                case SkillLineType.UnitDirection:
                    direction = this.RotateVector2(baseForward, nodeData.searchLineDirectionOffsetAngle);
                    width = nodeData.searchLineDirectionWidth;
                    length = nodeData.searchLineDirectionLength;
                    break;
                case SkillLineType.BetweenPoints:
                    startPos = context.GetPosition(nodeData.lineStartPositionSource, nodeData.lineStartBindingName);
                    Vector2 endPos = context.GetPosition(nodeData.lineEndPositionSource, nodeData.lineEndBindingName);
                    direction = (endPos - startPos).normalized;
                    width = nodeData.searchLineBetweenWidth;
                    length = Vector2.Distance(startPos, endPos);
                    break;
                default:
                    direction = this.RotateVector2(Vector2.right, nodeData.searchLineAbsoluteAngle);
                    width = nodeData.searchLineAbsoluteWidth;
                    length = nodeData.searchLineAbsoluteLength;
                    break;
            }

            Vector2 boxCenter = startPos + direction * (length * 0.5f);
            Vector2 boxSize = new Vector2(length, width);
            float boxAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            this.DebugDrawBox(boxCenter, boxSize, boxAngle);

            Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, boxAngle);
            foreach (Collider2D collider in colliders)
            {
                AbilitySystemComponent asc = this.GetASCFromCollider(collider);
                if (asc != null && this.IsValidTarget(asc))
                    foundTargets.Add(asc);
            }
        }

        private AbilitySystemComponent GetASCFromCollider(Collider2D collider)
        {
            return Collider2DRegistry.GetASC(collider);
        }

        private Vector2 RotateVector2(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(vector.x * cos - vector.y * sin, vector.x * sin + vector.y * cos);
        }

        private Vector2 GetFacingDirection(Transform casterTransform)
        {
            return casterTransform.localScale.x >= 0 ? Vector2.left : Vector2.right;
        }

        private bool IsValidTarget(AbilitySystemComponent target)
        {
            if (target == null)
                return false;

            AbilitySystemComponent currentTarget = this.Spec.GetTaskTarget();
            if (target == currentTarget)
                return false;

            var nodeData = this.NodeData as SearchTargetTaskNodeData;
            if (nodeData == null)
                return false;

            if (!nodeData.searchTargetTags.IsEmpty && !target.HasAnyTags(nodeData.searchTargetTags))
                return false;

            if (!nodeData.searchExcludeTags.IsEmpty && target.HasAnyTags(nodeData.searchExcludeTags))
                return false;

            return true;
        }

        private void DebugDrawCircle(Vector2 center, float radius)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
                return;

            const int segments = 32;
            float angleStep = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float angle1 = i * angleStep * Mathf.Deg2Rad;
                float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;
                Vector3 p1 = new(center.x + Mathf.Cos(angle1) * radius, center.y + Mathf.Sin(angle1) * radius, 0);
                Vector3 p2 = new(center.x + Mathf.Cos(angle2) * radius, center.y + Mathf.Sin(angle2) * radius, 0);
                Debug.DrawLine(p1, p2, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            }
        }

        private void DebugDrawSector(Vector2 center, Vector2 forward, float radius, float angle)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
                return;

            float halfAngle = angle * 0.5f;
            int arcSegments = Mathf.Max(8, (int)(angle / 10f));
            float angleStep = angle / arcSegments;
            float forwardAngle = Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg;
            float startAngle = forwardAngle - halfAngle;
            Vector3 center3D = new(center.x, center.y, 0);

            Vector3 leftEdge = center3D + new Vector3(
                Mathf.Cos((forwardAngle - halfAngle) * Mathf.Deg2Rad) * radius,
                Mathf.Sin((forwardAngle - halfAngle) * Mathf.Deg2Rad) * radius,
                0);
            Vector3 rightEdge = center3D + new Vector3(
                Mathf.Cos((forwardAngle + halfAngle) * Mathf.Deg2Rad) * radius,
                Mathf.Sin((forwardAngle + halfAngle) * Mathf.Deg2Rad) * radius,
                0);

            Debug.DrawLine(center3D, leftEdge, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(center3D, rightEdge, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);

            for (int i = 0; i < arcSegments; i++)
            {
                float angle1 = (startAngle + i * angleStep) * Mathf.Deg2Rad;
                float angle2 = (startAngle + (i + 1) * angleStep) * Mathf.Deg2Rad;
                Vector3 p1 = center3D + new Vector3(Mathf.Cos(angle1) * radius, Mathf.Sin(angle1) * radius, 0);
                Vector3 p2 = center3D + new Vector3(Mathf.Cos(angle2) * radius, Mathf.Sin(angle2) * radius, 0);
                Debug.DrawLine(p1, p2, SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            }
        }

        private void DebugDrawBox(Vector2 center, Vector2 size, float angle)
        {
            if (!SearchTargetTaskSpec.DebugDraw)
                return;

            float rad = angle * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            Vector2[] localCorners =
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, halfHeight)
            };

            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float rotatedX = localCorners[i].x * cos - localCorners[i].y * sin;
                float rotatedY = localCorners[i].x * sin + localCorners[i].y * cos;
                worldCorners[i] = new Vector3(center.x + rotatedX, center.y + rotatedY, 0);
            }

            Debug.DrawLine(worldCorners[0], worldCorners[1], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[1], worldCorners[2], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[2], worldCorners[3], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
            Debug.DrawLine(worldCorners[3], worldCorners[0], SearchTargetTaskSpec.DebugDrawColor, SearchTargetTaskSpec.DebugDrawDuration);
        }

        private bool ShouldLogSkill1001()
        {
            return this.Spec?.SkillId == "1001";
        }

        private string DescribeCollider(Collider2D collider)
        {
            if (collider == null)
            {
                return "null-collider";
            }

            Transform transform = collider.transform;
            return $"{collider.name}@{transform.position}";
        }

        private string DescribeTransform(Transform transform)
        {
            if (transform == null)
            {
                return "null-transform";
            }

            return $"{transform.name}@pos={transform.position},rot={transform.rotation.eulerAngles},scale={transform.localScale}";
        }

        private int GetUnitConfigId(AbilitySystemComponent asc)
        {
            SkillUnit skillUnit = asc?.GetParent<SkillUnit>();
            Unit unit = skillUnit?.Unit.As();
            return unit?.ConfigId ?? 0;
        }
    }
}
