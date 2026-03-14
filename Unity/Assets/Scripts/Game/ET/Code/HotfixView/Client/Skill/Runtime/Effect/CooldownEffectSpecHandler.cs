namespace ET.Client
{
    [FriendOf(typeof(GameplayEffectSpec))]
    [FriendOf(typeof(SpecExecutionContext))]
    [FriendOf(typeof(CooldownEffectSpec))]
    [FriendOf(typeof(AbilitySystemComponent))]
    public class CooldownEffectSpecHandler : AEffectHandler
    {
        public CooldownEffectSpec SelfSpec()
        {
            return Spec.GetComponent<CooldownEffectSpec>();
        }

        public CooldownEffectNodeData GetNode()
        {
            return NodeData as CooldownEffectNodeData;
        }

        public override SpecExecutionContext GetContext()
        {
            return Spec.GetContext();
        }

        public override void OnInitialize()
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            CooldownEffectNodeData nodeData = GetNode();
            if (nodeData == null || nodeData.cooldownType != CooldownType.Charge)
            {
                return;
            }

            selfSpec.MaxCharges = nodeData.maxCharges;
            selfSpec.ChargeTime = FormulaEvaluator.EvaluateSimple(nodeData.chargeTime, 10f);
            selfSpec.CurrentCharges = selfSpec.MaxCharges;
            selfSpec.ChargeTimer = 0f;
        }

        public override void Execute()
        {
            if (Spec.Context == null)
            {
                Log.Warning("[CooldownEffect] Context is null");
                return;
            }

            CooldownEffectNodeData nodeData = GetNode();
            if (nodeData == null)
            {
                Log.Warning("[CooldownEffect] CooldownNodeData is null");
                return;
            }

            if (nodeData.cooldownType == CooldownType.Normal)
            {
                Spec.Execute();
                return;
            }

            ExecuteChargeCooldown();
        }

        public void ExecuteChargeCooldown()
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            if (selfSpec.CurrentCharges <= 0)
            {
                return;
            }

            selfSpec.CurrentCharges--;
            if (selfSpec.ChargeTimer <= 0f && selfSpec.CurrentCharges < selfSpec.MaxCharges)
            {
                selfSpec.ChargeTimer = selfSpec.ChargeTime;
            }

            UpdateChargeCooldownTag();
            EnsureRegistered();
        }

        public void EnsureRegistered()
        {
            AbilitySystemComponent target = Spec.GetTarget();
            if (target == null || target.EffectContainer == null)
            {
                return;
            }

            Spec.Target = target;
            Spec.IsRunning = true;

            GameplayEffectSpec existingEffect = target.EffectContainer.FindEffectByNodeGuid(Spec.NodeGuid);
            if (existingEffect == null)
            {
                target.EffectContainer.AddEffect(Spec);
            }
        }

        public override void Tick(float deltaTime)
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            CooldownEffectNodeData nodeData = selfSpec.CooldownNodeData;
            if (nodeData == null)
            {
                return;
            }

            if (nodeData.cooldownType == CooldownType.Normal)
            {
                Spec.TickEffect(deltaTime);
                return;
            }

            TickChargeCooldown(deltaTime);
        }

        public void TickChargeCooldown(float deltaTime)
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            if (selfSpec.CurrentCharges >= selfSpec.MaxCharges || selfSpec.ChargeTimer <= 0f)
            {
                return;
            }

            selfSpec.ChargeTimer -= deltaTime;
            if (selfSpec.ChargeTimer > 0f)
            {
                return;
            }

            selfSpec.CurrentCharges++;
            selfSpec.ChargeTimer = selfSpec.CurrentCharges < selfSpec.MaxCharges ? selfSpec.ChargeTime : 0f;
            UpdateChargeCooldownTag();
        }

        public void UpdateChargeCooldownTag()
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            AbilitySystemComponent target = Spec.Target.As();
            if (target == null || Spec.Tags.GrantedTags.IsEmpty)
            {
                return;
            }

            if (selfSpec.CurrentCharges <= 0)
            {
                target.OwnedTags.AddTags(Spec.Tags.GrantedTags);
                return;
            }

            target.OwnedTags.RemoveTags(Spec.Tags.GrantedTags);
        }

        public override void Reset()
        {
            CooldownEffectSpec selfSpec = SelfSpec();
            Spec.ResetEffect();

            if (!selfSpec.IsChargeCooldown)
            {
                return;
            }

            selfSpec.CurrentCharges = selfSpec.MaxCharges;
            selfSpec.ChargeTimer = 0f;
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            return GetContext();
        }

        public override void Cancel()
        {
            Spec.CancelEffect();
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
