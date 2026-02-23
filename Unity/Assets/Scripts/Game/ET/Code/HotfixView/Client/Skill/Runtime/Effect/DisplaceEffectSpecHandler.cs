

using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 位移效果Spec - 持续移动目标位置（吸引/击退/吸引到指定点）
    /// 利用基类的 Duration/Tick 机制实现逐帧位移
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.DisplaceEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    public partial class DisplaceEffectSpecHandler : AEffectHandler
    {
        public DisplaceEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<DisplaceEffectSpec>();
            return selfSpec;
        }
        public DisplaceEffectNodeData GetNode()
        {
            var nodeData = NodeData as DisplaceEffectNodeData;
            return nodeData;
        }
        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void Cancel()
        {
            Spec.CancelEffect();
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override void OnCompleteHook()
        {
        }


        public override void OnInitialize()
        {
        }

        public override void OnPeriodicHook()
        {
        }

        public override void Reset()
        {
            Spec.ResetEffect();
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            if (target?.Owner == null) return;

            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData == null) return;

            selfSpec._targetTransform = target.Owner.transform;
            selfSpec._startPosition = selfSpec._targetTransform.position;
            selfSpec._movedDistance = 0f;

            var Context = GetContext();
            Vector3 casterPos = Context?.Caster.As().Owner != null
                ? Context.Caster.As().Owner.transform.position
                : selfSpec._startPosition;

            switch (nodeData.displaceType)
            {
                case DisplaceType.Pull:
                    // 吸引：方向指向施法者
                    selfSpec._targetPoint = casterPos;
                    selfSpec._displaceDirection = (casterPos - selfSpec._startPosition).normalized;
                    break;

                case DisplaceType.Push:
                    // 击退：方向远离施法者
                    selfSpec._displaceDirection = (selfSpec._startPosition - casterPos).normalized;
                    selfSpec._targetPoint = selfSpec._startPosition + selfSpec._displaceDirection * nodeData.distance;
                    break;

                case DisplaceType.PullToPoint:
                    // 吸引到指定点
                    selfSpec._targetPoint = Context.GetPosition(nodeData.pointSource, nodeData.pointBindingName);
                    selfSpec._displaceDirection = (selfSpec._targetPoint - selfSpec._startPosition).normalized;
                    break;
            }

            // 方向为零（目标和施法者重叠）时不位移
            if (selfSpec._displaceDirection.sqrMagnitude < 0.001f)
            {
                Spec.Expire();
            }
        }

        public override void Tick(float deltaTime)
        {
            // 先调用基类Tick处理超时
            Spec.TickEffect(deltaTime);

            var selfSpec = SelfSpec();
            if (Spec.IsExpired || !Spec.IsApplied || selfSpec._targetTransform == null) return;

            var nodeData = GetNode();
            if (nodeData == null) return;

            float moveStep = nodeData.speed * deltaTime;
            selfSpec._movedDistance += moveStep;

            // 检查是否到达最大距离
            if (selfSpec._movedDistance >= nodeData.distance)
            {
                Spec.Expire();
                return;
            }

            Vector3 currentPos = selfSpec._targetTransform.position;
            var Context = GetContext();
            switch (nodeData.displaceType)
            {
                case DisplaceType.Pull:
                    {
                        // 吸引：检查是否到达最小距离
                        Vector3 casterPos = Context?.Caster.As().Owner != null
                            ? Context.Caster.As().Owner.transform.position
                            : selfSpec._targetPoint;
                        float distToCaster = Vector3.Distance(currentPos, casterPos);
                        if (distToCaster <= nodeData.minDistance)
                        {
                            Spec.Expire();
                            return;
                        }
                        // 实时更新方向（施法者可能在移动）
                        selfSpec._displaceDirection = (casterPos - currentPos).normalized;
                        selfSpec._targetTransform.position = currentPos + selfSpec._displaceDirection * moveStep;
                        break;
                    }

                case DisplaceType.Push:
                    {
                        selfSpec._targetTransform.position = currentPos + selfSpec._displaceDirection * moveStep;
                        break;
                    }

                case DisplaceType.PullToPoint:
                    {
                        float distToPoint = Vector3.Distance(currentPos, selfSpec._targetPoint);
                        if (distToPoint <= nodeData.minDistance)
                        {
                            Spec.Expire();
                            return;
                        }
                        selfSpec._displaceDirection = (selfSpec._targetPoint - currentPos).normalized;
                        selfSpec._targetTransform.position = currentPos + selfSpec._displaceDirection * moveStep;
                        break;
                    }
            }
        }
    }
}
