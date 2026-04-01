using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 位移效果 Spec，运行时只依赖技能逻辑状态，不缓存 Unity Transform。
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.DisplaceEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    public partial class DisplaceEffectSpecHandler : AEffectHandler
    {
        public DisplaceEffectSpec SelfSpec()
        {
            return Spec.GetComponent<DisplaceEffectSpec>();
        }

        public DisplaceEffectNodeData GetNode()
        {
            return NodeData as DisplaceEffectNodeData;
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
            if (target == null)
            {
                return;
            }

            DisplaceEffectSpec selfSpec = SelfSpec();
            DisplaceEffectNodeData nodeData = GetNode();
            if (selfSpec == null || nodeData == null)
            {
                return;
            }

            selfSpec._targetAbility = target;
            selfSpec._startPosition = GetRuntimePosition(target);
            selfSpec._movedDistance = 0f;

            SpecExecutionContext context = GetContext();
            AbilitySystemComponent caster = context?.GetCaster();
            float3 casterPos = caster != null ? GetRuntimePosition(caster) : selfSpec._startPosition;

            switch (nodeData.displaceType)
            {
                case DisplaceType.Pull:
                    selfSpec._targetPoint = casterPos;
                    selfSpec._displaceDirection = math.normalizesafe(casterPos - selfSpec._startPosition);
                    break;

                case DisplaceType.Push:
                    selfSpec._displaceDirection = math.normalizesafe(selfSpec._startPosition - casterPos);
                    selfSpec._targetPoint = selfSpec._startPosition + selfSpec._displaceDirection * nodeData.distance;
                    break;

                case DisplaceType.PullToPoint:
                    selfSpec._targetPoint = ToFloat3(context?.GetPosition(nodeData.pointSource, nodeData.pointBindingName) ?? Vector3.zero);
                    selfSpec._displaceDirection = math.normalizesafe(selfSpec._targetPoint - selfSpec._startPosition);
                    break;
            }

            if (math.lengthsq(selfSpec._displaceDirection) < 0.001f)
            {
                Spec.Expire();
            }
        }

        public override void Tick(float deltaTime)
        {
            Spec.TickEffect(deltaTime);

            DisplaceEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null || Spec.IsExpired || !Spec.IsApplied)
            {
                return;
            }

            DisplaceEffectNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            AbilitySystemComponent targetAbility = selfSpec._targetAbility.As();
            Unit targetUnit = GetTargetUnit(targetAbility);
            if (targetUnit == null)
            {
                Spec.Expire();
                return;
            }

            float moveStep = nodeData.speed * deltaTime;
            selfSpec._movedDistance += moveStep;
            if (selfSpec._movedDistance >= nodeData.distance)
            {
                Spec.Expire();
                return;
            }

            float3 currentPos = targetUnit.Position;
            SpecExecutionContext context = GetContext();
            switch (nodeData.displaceType)
            {
                case DisplaceType.Pull:
                    {
                        AbilitySystemComponent caster = context?.GetCaster();
                        float3 casterPos = caster != null ? GetRuntimePosition(caster) : selfSpec._targetPoint;
                        float distToCaster = math.distance(currentPos, casterPos);
                        if (distToCaster <= nodeData.minDistance)
                        {
                            Spec.Expire();
                            return;
                        }

                        selfSpec._displaceDirection = math.normalizesafe(casterPos - currentPos);
                        ApplyRuntimePosition(currentPos + selfSpec._displaceDirection * moveStep, targetUnit);
                        break;
                    }

                case DisplaceType.Push:
                    ApplyRuntimePosition(currentPos + selfSpec._displaceDirection * moveStep, targetUnit);
                    break;

                case DisplaceType.PullToPoint:
                    {
                        float distToPoint = math.distance(currentPos, selfSpec._targetPoint);
                        if (distToPoint <= nodeData.minDistance)
                        {
                            Spec.Expire();
                            return;
                        }

                        selfSpec._displaceDirection = math.normalizesafe(selfSpec._targetPoint - currentPos);
                        ApplyRuntimePosition(currentPos + selfSpec._displaceDirection * moveStep, targetUnit);
                        break;
                    }
            }
        }

        private static Unit GetTargetUnit(AbilitySystemComponent target)
        {
            SkillUnit skillUnit = target?.GetParent<SkillUnit>();
            return skillUnit?.Unit.As();
        }

        private static void ApplyRuntimePosition(float3 nextPosition, Unit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.Position = nextPosition;
        }

        private static float3 GetRuntimePosition(AbilitySystemComponent asc)
        {
            UnityEngine.Transform ownerTransform = asc?.GetOwnerTransform();
            if (ownerTransform != null)
            {
                return ToFloat3(ownerTransform.position);
            }

            Unit unit = asc?.GetParent<SkillUnit>()?.Unit.As();
            return unit?.Position ?? float3.zero;
        }

        private static float3 ToFloat3(Vector3 value)
        {
            return new float3(value.x, value.y, value.z);
        }
    }
}
