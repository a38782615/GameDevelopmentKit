using UnityEngine;

namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.DamageEffectSpec))]
    public partial class DamageEffectSpecHandler : AEffectHandler
    {
        public DamageEffectSpec SelfSpec()
        {
            return Spec.GetComponent<DamageEffectSpec>();
        }

        public DamageEffectNodeData GetNode()
        {
            return NodeData as DamageEffectNodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            if (target == null)
            {
                return;
            }

            var nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            var context = GetContext();
            float baseDamage = CalculateDamage(nodeData, target);

            if (nodeData.damageMultiplyByStackCount)
            {
                int stackCount = context?.StackCount ?? 1;
                baseDamage *= stackCount;
            }

            if (nodeData.damageCalculationType == DamageCalculationType.Default
                && nodeData.damageType != DamageType.True
                && target.Attributes != null)
            {
                AttrType defenseType = nodeData.damageType == DamageType.Physical ? AttrType.Defense : AttrType.MagicDefense;
                float? defense = target.Attributes.GetCurrentValue(defenseType);
                if (defense.HasValue && defense.Value > 0)
                {
                    baseDamage *= 100f / (100f + defense.Value);
                }
            }

            baseDamage = Mathf.Max(0f, baseDamage);

            if (target.Attributes != null && baseDamage > 0f)
            {
                Attribute healthAttr = target.Attributes.GetAttribute(AttrType.Health);
                if (healthAttr != null)
                {
                    healthAttr.BaseValue -= baseDamage;
                }

                SpecExecutionContext executionContext = GetExecutionContext();
                var damageResult = new DamageResult(baseDamage, false, false, nodeData.damageType);
                executionContext.SetCustomData("DamageResult", damageResult);
            }

            if (baseDamage > 0f)
            {
                InitializeKnockback(target, nodeData);
            }
        }

        private float CalculateDamage(DamageEffectNodeData nodeData, AbilitySystemComponent target)
        {
            var context = GetContext();
            switch (nodeData.damageSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    return nodeData.damageFixedValue;

                case ModifierMagnitudeSourceType.Formula:
                    return FormulaEvaluator.Evaluate(nodeData.damageFormula, new FormulaContext
                    {
                        CasterAttributes = context?.Caster.As()?.Attributes,
                        TargetAttributes = target.Attributes,
                        Level = Spec.Level,
                        StackCount = context?.StackCount ?? 1
                    });

                case ModifierMagnitudeSourceType.SetByCaller:
                    return Spec.GetSetByCallerValue(nodeData.damageSetByCallerKey, 0f);

                case ModifierMagnitudeSourceType.ModifierMagnitudeCalculation:
                    return CalculateMMCDamage(nodeData, target);

                default:
                    return 0f;
            }
        }

        private float CalculateMMCDamage(DamageEffectNodeData nodeData, AbilitySystemComponent target)
        {
            if (nodeData.damageMMCType == MMCType.AttributeBased)
            {
                float? attrValue = null;

                if (nodeData.damageMMCUseSnapshot && Spec.SnapshotValues != null)
                {
                    if (Spec.SnapshotValues.TryGetValue(nodeData.damageMMCCaptureAttribute, out float snapshotValue))
                    {
                        attrValue = snapshotValue;
                    }
                }

                if (!attrValue.HasValue)
                {
                    if (nodeData.damageMMCAttributeSource == MMCAttributeSource.Source)
                    {
                        attrValue = Spec.Source.As()?.Attributes?.GetCurrentValue(nodeData.damageMMCCaptureAttribute);
                    }
                    else
                    {
                        attrValue = target?.Attributes?.GetCurrentValue(nodeData.damageMMCCaptureAttribute);
                    }
                }

                return (attrValue ?? 0f) * nodeData.damageMMCCoefficient;
            }

            if (nodeData.damageMMCType == MMCType.LevelBased)
            {
                return nodeData.damageFixedValue * (1 + Spec.Level * 0.1f);
            }

            return nodeData.damageFixedValue;
        }

        public override void Cancel()
        {
            Spec.CancelEffect();
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return Spec.GetContext();
        }

        public override void OnCompleteHook()
        {
        }

        public override void OnInitialize()
        {
            DamageEffectSpec selfSpec = SelfSpec();
            DamageEffectNodeData nodeData = GetNode();
            if (selfSpec == null)
            {
                return;
            }

            selfSpec.HasRuntimeFollowup = CanEnableHitKnockback(nodeData);
            selfSpec.KnockbackDirection = Vector3.zero;
            selfSpec.KnockbackRemainingDistance = 0f;
            selfSpec.KnockbackSpeed = 0f;
            selfSpec.KnockbackTransform = null;

            if (selfSpec.HasRuntimeFollowup)
            {
                selfSpec.KnockbackSpeed = Mathf.Max(0.01f, nodeData.knockbackSpeed);
                Spec.Duration = nodeData.knockbackDistance / selfSpec.KnockbackSpeed;
            }
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

            DamageEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null || Spec.IsExpired || !Spec.IsApplied || !selfSpec.HasRuntimeFollowup)
            {
                return;
            }

            if (selfSpec.KnockbackTransform == null
                || selfSpec.KnockbackRemainingDistance <= 0f
                || selfSpec.KnockbackSpeed <= 0f)
            {
                Spec.Expire();
                return;
            }

            float moveStep = Mathf.Min(selfSpec.KnockbackSpeed * deltaTime, selfSpec.KnockbackRemainingDistance);
            if (moveStep <= 0f)
            {
                Spec.Expire();
                return;
            }

            Vector3 currentPosition = selfSpec.KnockbackTransform.position;
            Vector3 nextPosition = currentPosition + selfSpec.KnockbackDirection * moveStep;
            nextPosition.z = currentPosition.z;
            selfSpec.KnockbackTransform.position = nextPosition;
            selfSpec.KnockbackRemainingDistance -= moveStep;

            if (selfSpec.KnockbackRemainingDistance <= 0.001f)
            {
                Spec.Expire();
            }
        }

        private bool CanEnableHitKnockback(DamageEffectNodeData nodeData)
        {
            return nodeData != null
                && nodeData.enableHitKnockback
                && nodeData.knockbackDistance > 0f
                && nodeData.knockbackSpeed > 0f;
        }

        private void InitializeKnockback(AbilitySystemComponent target, DamageEffectNodeData nodeData)
        {
            DamageEffectSpec selfSpec = SelfSpec();
            if (selfSpec == null || !selfSpec.HasRuntimeFollowup || target?.Owner == null)
            {
                return;
            }

            Transform targetTransform = target.Owner.transform;
            Vector3 targetPosition = targetTransform.position;
            Vector3 casterPosition = targetPosition;

            var context = GetContext();
            GameObject casterObject = context?.Caster.As()?.Owner;
            if (casterObject != null)
            {
                casterPosition = casterObject.transform.position;
            }

            Vector3 knockbackDirection = targetPosition - casterPosition;
            knockbackDirection.z = 0f;

            if (knockbackDirection.sqrMagnitude < 0.0001f)
            {
                knockbackDirection = targetTransform.right;
                knockbackDirection.z = 0f;
            }

            if (knockbackDirection.sqrMagnitude < 0.0001f)
            {
                knockbackDirection = Vector3.right;
            }

            selfSpec.KnockbackTransform = targetTransform;
            selfSpec.KnockbackDirection = knockbackDirection.normalized;
            selfSpec.KnockbackRemainingDistance = Mathf.Max(0f, nodeData.knockbackDistance);
        }
    }
}
