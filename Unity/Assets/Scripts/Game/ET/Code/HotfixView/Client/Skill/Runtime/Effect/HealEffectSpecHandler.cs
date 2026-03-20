
namespace ET.Client
{
    /// <summary>
    /// 治疗效果 Spec（瞬时效果）
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    public partial class HealEffectSpecHandler : AEffectHandler
    {
        public HealEffectSpec SelfSpec()
        {
            HealEffectSpec selfSpec = Spec.GetComponent<HealEffectSpec>();
            return selfSpec;
        }

        public HealEffectNodeData GetNode()
        {
            HealEffectNodeData nodeData = NodeData as HealEffectNodeData;
            return nodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
        }

        public override void Execute()
        {
            Spec.Execute();
        }

        public override void Cancel()
        {
            Spec.CancelEffect();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void OnCompleteHook()
        {
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
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            HealEffectNodeData healNodeData = GetNode();
            if (target?.Attributes == null || healNodeData == null)
            {
                return;
            }

            float currentHealth = target.Attributes.GetCurrentValue(global::ET.NumericType.Hp);
            float maxHealth = target.Attributes.GetCurrentValue(global::ET.NumericType.MaxHp);
            if (maxHealth > 0f && currentHealth >= maxHealth - 0.001f)
            {
                return;
            }

            float baseHeal = CalculateHeal(healNodeData, target);
            if (healNodeData.healMultiplyByStackCount)
            {
                int stackCount = GetContext()?.GetStackCount() ?? 1;
                baseHeal *= stackCount;
            }

            baseHeal = UnityEngine.Mathf.Max(0f, baseHeal);
            if (baseHeal <= 0f)
            {
                return;
            }

            AttrCmp healthAttr = target.Attributes.GetAttribute(global::ET.NumericType.Hp);
            if (healthAttr == null)
            {
                return;
            }

            float oldHealth = healthAttr.BaseValue;
            float newHealth = oldHealth + baseHeal;
            if (maxHealth > 0f)
            {
                newHealth = UnityEngine.Mathf.Min(newHealth, maxHealth);
            }

            float actualHeal = UnityEngine.Mathf.Max(0f, newHealth - oldHealth);
            if (actualHeal <= 0.001f)
            {
                return;
            }

            target.Attributes.SetBaseValue(healthAttr.NumericType, newHealth);

            SpecExecutionContext executionContext = GetExecutionContext();
            executionContext.SetCustomData("Heal", actualHeal);
            executionContext.ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, SkillPortId.Effect.Initial);

        }

        /// <summary>
        /// 计算治疗量
        /// </summary>
        private float CalculateHeal(HealEffectNodeData nodeData, AbilitySystemComponent target)
        {
            SpecExecutionContext context = GetContext();
            switch (nodeData.healSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    return nodeData.healFixedValue;

                case ModifierMagnitudeSourceType.Formula:
                    if (string.IsNullOrEmpty(nodeData.healFormula))
                    {
                        return 0f;
                    }

                    return FormulaEvaluator.Evaluate(nodeData.healFormula, new FormulaContext
                    {
                        CasterAttributes = context?.GetCaster()?.Attributes,
                        TargetAttributes = target.Attributes,
                        Level = Spec.Level,
                        StackCount = context?.GetStackCount() ?? 1
                    });

                case ModifierMagnitudeSourceType.SetByCaller:
                    return Spec.GetSetByCallerValue(nodeData.healSetByCallerKey, 0f);

                case ModifierMagnitudeSourceType.ModifierMagnitudeCalculation:
                    return CalculateMMCHeal(nodeData, target);

                default:
                    return 0f;
            }
        }

        /// <summary>
        /// 使用 MMC 计算治疗量
        /// </summary>
        private float CalculateMMCHeal(HealEffectNodeData nodeData, AbilitySystemComponent target)
        {
            if (nodeData.healMMCType == MMCType.AttributeBased)
            {
                float? attrValue = null;

                if (nodeData.healMMCUseSnapshot && Spec.SnapshotValues != null)
                {
                    if (Spec.SnapshotValues.TryGetValue(nodeData.healMMCCaptureAttribute, out float snapshotValue))
                    {
                        attrValue = snapshotValue;
                    }
                }

                if (!attrValue.HasValue)
                {
                    if (nodeData.healMMCAttributeSource == MMCAttributeSource.Source)
                    {
                        attrValue = Spec.Source.As().Attributes?.GetCurrentValue(nodeData.healMMCCaptureAttribute);
                    }
                    else
                    {
                        attrValue = target?.Attributes?.GetCurrentValue(nodeData.healMMCCaptureAttribute);
                    }
                }

                return (attrValue ?? 0f) * nodeData.healMMCCoefficient;
            }

            if (nodeData.healMMCType == MMCType.LevelBased)
            {
                return nodeData.healFixedValue * (1 + Spec.Level * 0.1f);
            }

            return nodeData.healFixedValue;
        }
    }
}
