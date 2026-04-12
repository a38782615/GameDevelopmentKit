using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.DamageEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]
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

            DamageEffectNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                return;
            }

            SpecExecutionContext context = GetContext();
            float baseDamage = CalculateDamage(nodeData, target);

            if (nodeData.damageMultiplyByStackCount)
            {
                int stackCount = context?.GetStackCount() ?? 1;
                baseDamage *= stackCount;
            }

            if (nodeData.damageCalculationType == DamageCalculationType.Default
                && nodeData.damageType != DamageType.True
                && target.Attributes != null)
            {
                int defenseType = nodeData.damageType == DamageType.Physical ? global::ET.NumericType.Armor : global::ET.NumericType.MagicResistance;
                float? defense = target.Attributes.GetCurrentValue(defenseType);
                if (defense.HasValue && defense.Value > 0)
                {
                    baseDamage *= 100f / (100f + defense.Value);
                }
            }

            baseDamage = Mathf.Max(0f, baseDamage);

            if (target.Attributes != null && baseDamage > 0f)
            {
                AttrCmp healthAttr = target.Attributes.GetAttribute(global::ET.NumericType.Hp);
                if (healthAttr != null)
                {
                    target.Attributes.SetBaseValue(healthAttr.NumericType, healthAttr.CurrentValue - baseDamage);
                }

                SpecExecutionContext executionContext = GetExecutionContext();
                DamageResult damageResult = new DamageResult(baseDamage, false, false, nodeData.damageType);
                executionContext.SetCustomData("DamageResult", damageResult);
            }

            if (baseDamage > 0f)
            {
                InitializeKnockback(target, nodeData);
            }
        }

        private float CalculateDamage(DamageEffectNodeData nodeData, AbilitySystemComponent target)
        {
            SpecExecutionContext context = GetContext();
            switch (nodeData.damageSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    return nodeData.damageFixedValue;

                case ModifierMagnitudeSourceType.Formula:
                    FormulaContext formulaContext = FormulaContext.FromExecutionContext(context, target);
                    formulaContext.Level = Spec.Level;
                    return FormulaEvaluator.Evaluate(nodeData.damageFormula, formulaContext);

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
            SpecExecutionContext context = GetContext();
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
                        attrValue = context?.CreateModifierContext(target)?.GetSourceAttribute(nodeData.damageMMCCaptureAttribute);
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
            SpecExecutionContext context = GetContext();
            DamageEffectNodeData nodeData = GetNode();
            if (selfSpec == null)
            {
                return;
            }

            selfSpec.HasRuntimeFollowup = CanEnableHitKnockback(nodeData);
            selfSpec.KnockbackDirection = float3.zero;
            selfSpec.KnockbackRemainingDistance = 0f;
            selfSpec.KnockbackSpeed = 0f;
            selfSpec.KnockbackTarget = default;

            if (selfSpec.HasRuntimeFollowup)
            {
                if (context != null)
                {
                    SpecExecutionContext effectContext = context.CreateOwnedEffectContext(Spec);
                    if (effectContext != null)
                    {
                        Spec.Context = effectContext;
                    }
                }

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

            AbilitySystemComponent knockbackTarget = selfSpec.KnockbackTarget.As();
            if (knockbackTarget == null
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

            Unit targetUnit = GetTargetUnit(knockbackTarget);
            if (targetUnit == null)
            {
                Spec.Expire();
                return;
            }

            float3 currentPosition = targetUnit.Position;
            float3 nextPosition = currentPosition + selfSpec.KnockbackDirection * moveStep;
            nextPosition.z = currentPosition.z;
            ApplyKnockbackPosition(nextPosition, targetUnit);
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
            if (selfSpec == null || !selfSpec.HasRuntimeFollowup || target == null)
            {
                return;
            }

            float3 targetPosition = GetRuntimePosition(target);
            float3 casterPosition = targetPosition;

            SpecExecutionContext context = GetContext();
            AbilitySystemComponent caster = context?.GetCaster();
            if (caster != null)
            {
                casterPosition = GetRuntimePosition(caster);
            }

            float3 knockbackDirection = targetPosition - casterPosition;
            knockbackDirection.z = 0f;

            if (math.lengthsq(knockbackDirection) < 0.0001f)
            {
                knockbackDirection = new float3(1f, 0f, 0f);
            }

            if (math.lengthsq(knockbackDirection) < 0.0001f)
            {
                knockbackDirection = new float3(1f, 0f, 0f);
            }

            selfSpec.KnockbackTarget = target;
            selfSpec.KnockbackDirection = math.normalize(knockbackDirection);
            selfSpec.KnockbackRemainingDistance = Mathf.Max(0f, nodeData.knockbackDistance);
        }

        private Unit GetTargetUnit(AbilitySystemComponent target)
        {
            SkillUnit skillUnit = target?.GetParent<SkillUnit>();
            return skillUnit?.Unit.As();
        }

        private static void ApplyKnockbackPosition(float3 nextPosition, Unit unit)
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
                Vector3 ownerPosition = ownerTransform.position;
                return new float3(ownerPosition.x, ownerPosition.y, ownerPosition.z);
            }

            Unit unit = asc?.GetParent<SkillUnit>()?.Unit.As();
            return unit?.Position ?? float3.zero;
        }

    }
}
