using System.Collections.Generic;


namespace ET.Client
{
    /// <summary>
    /// Buff效果Spec（持续效果）
    /// </summary>
    [FriendOfAttribute(typeof(ET.Client.GameplayEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.SpecExecutionContext))]
    [FriendOfAttribute(typeof(ET.Client.CooldownEffectSpec))]
    [FriendOfAttribute(typeof(ET.Client.AbilitySystemComponent))]
    public class CooldownEffectSpecHandler : AEffectHandler
    {
        public CooldownEffectSpec SelfSpec()
        {
            var selfSpec = Spec.GetComponent<CooldownEffectSpec>();
            return selfSpec;
        }
        public CooldownEffectNodeData GetNode()
        {
            var nodeData = NodeData as CooldownEffectNodeData;
            return nodeData;
        }
        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
            var selfSpec = SelfSpec();
            var nodeData = GetNode();
            if (nodeData != null && nodeData.cooldownType == CooldownType.Charge)
            {
                selfSpec.MaxCharges = nodeData.maxCharges;
                selfSpec.ChargeTime = FormulaEvaluator.EvaluateSimple(nodeData.chargeTime, 10f);
                selfSpec.CurrentCharges = selfSpec.MaxCharges;  // 初始满充能
                selfSpec.ChargeTimer = 0f;
            }
        }

        public override void Execute()
        {
            if (Spec.Context == null)
            {
                UnityEngine.Debug.LogWarning($"[CooldownEffect] Context 为空");
                return;
            }

            var nodeData = GetNode();
            if (nodeData == null)
            {
                UnityEngine.Debug.LogWarning($"[CooldownEffect] CooldownNodeData 为空");
                return;
            }

            UnityEngine.Debug.Log($"[CooldownEffect] 执行CD效果, SkillId: {Spec.SkillId}, CooldownType: {nodeData.cooldownType}, GrantedTags: [{string.Join(", ", Spec.Tags.GrantedTags.Tags ?? new GameplayTag[0])}]");

            if (nodeData.cooldownType == CooldownType.Normal)
            {
                // ========== 普通CD模式 ==========
                // 调用基类的 Execute，走标准的持续效果流程
                Spec.Execute();
                UnityEngine.Debug.Log($"[CooldownEffect] 普通CD执行完成, Target: {Spec.Target.As().Owner.name}, IsRunning: {Spec.IsRunning}");
            }
            else
            {
                // ========== 充能CD模式 ==========
                ExecuteChargeCooldown();
            }
        }

        /// <summary>
        /// 执行充能CD逻辑
        /// </summary>
        public void ExecuteChargeCooldown()
        {
            var selfSpec = SelfSpec();
            if (selfSpec.CurrentCharges > 0)
            {
                selfSpec.CurrentCharges--;

                // 如果之前是满的，开始充能计时
                if (selfSpec.ChargeTimer <= 0 && selfSpec.CurrentCharges < selfSpec.MaxCharges)
                {
                    selfSpec.ChargeTimer = selfSpec.ChargeTime;
                }

                // 更新 CD 标签状态
                UpdateChargeCooldownTag();

                // 确保 Effect 被注册到 Container 以便 Tick
                EnsureRegistered();
            }
        }

        /// <summary>
        /// 确保充能效果被注册到 EffectContainer
        /// </summary>
        public void EnsureRegistered()
        {
            var target = Spec.GetTarget();
            if (target == null) return;

            Spec.Target = target;
            Spec.IsRunning = true;

            // 检查是否已经注册
            var existingEffect = target.EffectContainer.FindEffectByNodeGuid(Spec.NodeGuid);
            if (existingEffect == null)
            {
                target.EffectContainer.AddEffect(Spec);
            }
        }

        public override void Tick(float deltaTime)
        {
            var selfSpec = SelfSpec();
            var nodeData = selfSpec.CooldownNodeData;
            if (nodeData == null) return;

            if (nodeData.cooldownType == CooldownType.Normal)
            {
                // 普通CD：调用基类 Tick（处理持续时间）
                Spec.TickEffect(deltaTime);
            }
            else
            {
                // 充能CD：处理充能恢复
                TickChargeCooldown(deltaTime);
            }
        }

        /// <summary>
        /// 充能CD的Tick逻辑
        /// </summary>
        public void TickChargeCooldown(float deltaTime)
        {
            var selfSpec = SelfSpec();
            // 未满时才计时
            if (selfSpec.CurrentCharges < selfSpec.MaxCharges && selfSpec.ChargeTimer > 0)
            {
                selfSpec.ChargeTimer -= deltaTime;

                if (selfSpec.ChargeTimer <= 0)
                {
                    // 恢复一层充能
                    selfSpec.CurrentCharges++;

                    // 还没满，继续计时
                    if (selfSpec.CurrentCharges < selfSpec.MaxCharges)
                    {
                        selfSpec.ChargeTimer = selfSpec.ChargeTime;
                    }
                    else
                    {
                        selfSpec.ChargeTimer = 0f;
                    }

                    // 更新 CD 标签状态
                    UpdateChargeCooldownTag();
                }
            }
        }

        /// <summary>
        /// 更新充能CD的标签状态
        /// </summary>
        public void UpdateChargeCooldownTag()
        {
            var selfSpec = SelfSpec();
            var Target = Spec.Target.As();
            if (Target == null) return;

            if (selfSpec.CurrentCharges <= 0)
            {
                // 没有充能了，添加 CD 标签阻止技能释放
                if (!Spec.Tags.GrantedTags.IsEmpty)
                {
                    Target.OwnedTags.AddTags(Spec.Tags.GrantedTags);
                }
            }
            else
            {
                // 有充能，移除 CD 标签允许技能释放
                if (!Spec.Tags.GrantedTags.IsEmpty)
                {
                    Target.OwnedTags.RemoveTags(Spec.Tags.GrantedTags);
                }
            }
        }

        public override void Reset()
        {
            var selfSpec = SelfSpec();
            Spec.ResetEffect();

            if (selfSpec.IsChargeCooldown)
            {
                selfSpec.CurrentCharges = selfSpec.MaxCharges;
                selfSpec.ChargeTimer = 0f;
            }
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void Cancel()
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
    }
}
