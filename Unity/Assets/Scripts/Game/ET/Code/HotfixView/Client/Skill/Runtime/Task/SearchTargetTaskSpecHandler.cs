

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 结束技能任务Spec
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.TaskSpec))]
    [FriendOfAttribute(typeof(ET.Client.SearchTargetTaskSpec))]
    public class SearchTargetTaskSpecHandler : ATaskSpecHandler
    {
        private SearchTargetTaskNodeData GetNode()
        {
            return NodeData as SearchTargetTaskNodeData;
        }

        public SearchTargetTaskSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<SearchTargetTaskSpec>();
            if (selfSpec == null)
            {
                Spec.AddComponent<SearchTargetTaskSpec>();
            }
            return selfSpec;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.Context;
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return Spec.Context;
        }

        public override void OnInitialize()
        {
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void OnCompleteHook()
        {
        }

        public override void Execute()
        {
            var selfSpec = SelfSpec();
            selfSpec._foundTargets.Clear();

            var nodeData = GetNode();
            if (nodeData == null)
            {
                var context = GetExecutionContext();
                context.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "无目标");
                return;
            }

            var Context = GetContext();

            // 使用 PositionSourceType 获取检测中心位置
            Vector2 centerPosition = Context.GetPosition(nodeData.positionSource, nodeData.positionBindingName);

            // 获取位置来源对象（用于获取朝向）
            GameObject sourceObject = Context.GetSourceObject(nodeData.positionSource);
            Transform centerTransform = sourceObject?.transform;

            switch (nodeData.searchShapeType)
            {
                case SearchShapeType.Circle:
                    SearchCircle(centerPosition, nodeData.searchCircleRadius);
                    break;
                case SearchShapeType.Sector:
                    if (centerTransform != null)
                    {
                        var sectorForward = GetFacingDirection(centerTransform);
                        SearchSector(centerPosition, centerTransform, nodeData.searchSectorRadius, nodeData.searchSectorAngle);
                    }
                    break;
                case SearchShapeType.Line:
                    SearchLine(centerPosition, centerTransform);
                    break;
            }

            if (nodeData.maxTargets > 0 && selfSpec._foundTargets.Count > nodeData.maxTargets)
                selfSpec._foundTargets.RemoveRange(nodeData.maxTargets, selfSpec._foundTargets.Count - nodeData.maxTargets);

            var ctx = GetExecutionContext();
            if (selfSpec._foundTargets.Count == 0)
            {
                ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "无目标");
            }
            else
            {
                // 为每个目标创建带有 ParentInputTarget 的上下文并执行
                foreach (var findTarget in selfSpec._foundTargets)
                {
                    var targetCtx = ctx.CreateWithParentInput(findTarget);
                    targetCtx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "对每个目标");
                }
            }

            // 执行完成效果
            ctx.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "完成效果");
        }

        /// <summary>
        /// 圆形范围检测 - 使用Physics2D.OverlapCircleAll
        /// </summary>
        private void SearchCircle(Vector2 center, float radius)
        {
            var selfSpec = SelfSpec();
            var colliders = Physics2D.OverlapCircleAll(center, radius);
            foreach (var collider in colliders)
            {
                var asc = GetASCFromCollider(collider);
                if (asc != null && IsValidTarget(asc))
                {
                    selfSpec._foundTargets.Add(asc);
                }
            }
        }

        /// <summary>
        /// 扇形范围检测 - 先用圆形检测，再过滤角度
        /// </summary>
        private void SearchSector(Vector2 center, Transform casterTransform, float radius, float angle)
        {
            var selfSpec = SelfSpec();
            float halfAngle = angle * 0.5f;

            // 获取角色朝向（角色默认朝左，所以使用 -transform.right）
            Vector2 forward = GetFacingDirection(casterTransform);

            var colliders = Physics2D.OverlapCircleAll(center, radius);
            foreach (var collider in colliders)
            {
                var asc = GetASCFromCollider(collider);
                if (asc == null || !IsValidTarget(asc)) continue;

                // 计算到目标的方向
                Vector2 toTarget = (Vector2)collider.transform.position - center;

                // 跳过距离为0的情况（自己或重叠的目标）
                if (toTarget.sqrMagnitude < 0.001f) continue;

                // 检查是否在扇形角度内
                float angleToTarget = Vector2.Angle(forward, toTarget);

                if (angleToTarget <= halfAngle)
                {
                    selfSpec._foundTargets.Add(asc);
                }
            }
        }

        /// <summary>
        /// 直线/矩形范围检测 - 使用Physics2D.OverlapBoxAll
        /// </summary>
        private void SearchLine(Vector2 center, Transform casterTransform)
        {
            var nodeData = GetNode();
            if (nodeData == null) return;

            var selfSpec = SelfSpec();
            var Context = GetContext();
            Vector2 direction;
            float width, length;
            Vector2 startPos = center;

            // 获取角色朝向（角色默认朝左，所以使用 -transform.right）
            Vector2 baseForward = casterTransform != null ? GetFacingDirection(casterTransform) : Vector2.right;

            switch (nodeData.searchLineType)
            {
                case SkillLineType.UnitDirection:
                    direction = RotateVector2(baseForward, nodeData.searchLineDirectionOffsetAngle);
                    width = nodeData.searchLineDirectionWidth;
                    length = nodeData.searchLineDirectionLength;
                    break;
                case SkillLineType.BetweenPoints:
                    // 使用 PositionSourceType 获取起点和终点位置
                    startPos = Context.GetPosition(nodeData.lineStartPositionSource, nodeData.lineStartBindingName);
                    Vector2 endPos = Context.GetPosition(nodeData.lineEndPositionSource, nodeData.lineEndBindingName);
                    direction = (endPos - startPos).normalized;
                    width = nodeData.searchLineBetweenWidth;
                    length = Vector2.Distance(startPos, endPos);
                    break;
                default:
                    direction = RotateVector2(Vector2.right, nodeData.searchLineAbsoluteAngle);
                    width = nodeData.searchLineAbsoluteWidth;
                    length = nodeData.searchLineAbsoluteLength;
                    break;
            }

            // 计算Box的中心点和尺寸
            Vector2 boxCenter = startPos + direction * (length * 0.5f);
            Vector2 boxSize = new Vector2(length, width);
            float boxAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // 调试绘制

            var colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, boxAngle);
            foreach (var collider in colliders)
            {
                var asc = GetASCFromCollider(collider);
                if (asc != null && IsValidTarget(asc))
                {
                    selfSpec._foundTargets.Add(asc);
                }
            }
        }

        /// <summary>
        /// 从Collider2D获取AbilitySystemComponent
        /// </summary>
        private AbilitySystemComponent GetASCFromCollider(Collider2D collider)
        {
            if (collider == null) return null;

            // 尝试从GameObject获取Unit，再获取ASC
            var unit = collider.GetComponent<SkillUnit>();
            if (unit != null) return unit.ASC;

            // 尝试从父对象获取
            unit = collider.GetComponentInParent<SkillUnit>();
            if (unit != null) return unit.ASC;

            return null;
        }

        /// <summary>
        /// 旋转2D向量
        /// </summary>
        private Vector2 RotateVector2(Vector2 v, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        /// <summary>
        /// 获取角色朝向（角色默认朝左）
        /// scale.x >= 0 时朝左，scale.x < 0 时朝右
        /// </summary>
        private Vector2 GetFacingDirection(Transform casterTransform)
        {
            // 角色默认朝左，所以：
            // scale.x >= 0（默认）-> 朝左 -> Vector2.left
            // scale.x < 0（翻转）-> 朝右 -> Vector2.right
            return casterTransform.localScale.x >= 0 ? Vector2.left : Vector2.right;
        }

        private bool IsValidTarget(AbilitySystemComponent target)
        {
            if (target == null) return false;
            if (target == Spec.GetTarget()) return false;
            var nodeData = GetNode();
            if (nodeData == null) return false;
            if (!nodeData.searchTargetTags.IsEmpty && !target.HasAnyTags(nodeData.searchTargetTags)) return false;
            if (!nodeData.searchExcludeTags.IsEmpty && target.HasAnyTags(nodeData.searchExcludeTags)) return false;
            return true;
        }
    }
}
