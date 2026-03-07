

namespace ET.Client
{
    /// <summary>
    /// 治疗效果Spec（瞬时效果）
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    public partial class HealEffectSpecHandler : AEffectHandler
    {
        public HealEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<HealEffectSpec>();
            return selfSpec;
        }
        public HealEffectNodeData GetNode()
        {
            var nodeData = NodeData as HealEffectNodeData;
            return nodeData;
        }
        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }
        public override void OnInitialize()
        {
            Spec.OnInitialize();
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
            var HealNodeData = GetNode();
            if (target?.Attributes == null) return;
            var nodeData = HealNodeData;
            if (nodeData == null) return;
            if (target.Attributes.GetCurrentValue(AttrType.Health) == target.Attributes.GetCurrentValue(AttrType.MaxHealth))
            {
                return;
            }
            // 计算治疗量
            float baseHeal = CalculateHeal(nodeData, target);

            // 如果勾选了"乘以堆叠层数"，从上下文获取层数并相乘
            if (nodeData.healMultiplyByStackCount)
            {
                int stackCount = GetContext()?.StackCount ?? 1;
                baseHeal *= stackCount;
            }

            baseHeal = UnityEngine.Mathf.Max(0f, baseHeal);
            if (baseHeal <= 0) return;

            // 应用治疗
            var healthAttr = target.Attributes.GetAttribute(AttrType.Health);
            if (healthAttr == null) return;

            float? maxHealth = target.Attributes.GetCurrentValue(AttrType.MaxHealth);
            float newHealth = healthAttr.BaseValue + baseHeal;
            if (maxHealth.HasValue)
                newHealth = UnityEngine.Mathf.Min(newHealth, maxHealth.Value);

            // 只修改 BaseValue，CurrentValue 会自动重新计算
            healthAttr.BaseValue = newHealth;

            // 将治疗量存入上下文，供飘字Cue使用
            var ctx = GetExecutionContext();
            ctx.SetCustomData("Heal", baseHeal);

            GetExecutionContext().ExecuteConnectedNodes(Spec.SkillId, Spec.NodeGuid, "治疗");
        }

        /// <summary>
        /// 计算治疗量
        /// </summary>
        private float CalculateHeal(HealEffectNodeData nodeData, AbilitySystemComponent target)
        {
            var Context = GetContext();
            switch (nodeData.healSourceType)
            {
                case ModifierMagnitudeSourceType.FixedValue:
                    return nodeData.healFixedValue;

                case ModifierMagnitudeSourceType.Formula:
                    if (string.IsNullOrEmpty(nodeData.healFormula))
                        return 0f;
                    return FormulaEvaluator.Evaluate(nodeData.healFormula, new FormulaContext
                    {
                        CasterAttributes = Context?.Caster.As().Attributes,
                        TargetAttributes = target.Attributes,
                        Level = Spec.Level,
                        StackCount = Context?.StackCount ?? 1
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
                // 基于属性的 MMC
                float? attrValue = null;

                // 根据快照设置决定使用快照值还是实时值
                if (nodeData.healMMCUseSnapshot && Spec.SnapshotValues != null)
                {
                    if (Spec.SnapshotValues.TryGetValue(nodeData.healMMCCaptureAttribute, out float snapshotValue))
                    {
                        attrValue = snapshotValue;
                    }
                }

                // 如果没有快照值，实时获取
                if (!attrValue.HasValue)
                {
                    if (nodeData.healMMCAttributeSource == MMCAttributeSource.Source)
                    {
                        // 从施法者获取属性
                        attrValue = Spec.Source.As().Attributes?.GetCurrentValue(nodeData.healMMCCaptureAttribute);
                    }
                    else
                    {
                        // 从目标获取属性
                        attrValue = target?.Attributes?.GetCurrentValue(nodeData.healMMCCaptureAttribute);
                    }
                }

                // 属性值 × 系数
                return (attrValue ?? 0f) * nodeData.healMMCCoefficient;
            }
            else if (nodeData.healMMCType == MMCType.LevelBased)
            {
                // 基于等级的 MMC
                return nodeData.healFixedValue * (1 + Spec.Level * 0.1f);
            }

            return nodeData.healFixedValue;
        }
    }
}
