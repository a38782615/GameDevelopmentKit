

namespace ET.Client
{
    /// <summary>
    /// 投射物效果Spec
    /// 负责生成投射物并管理其生命周期
    /// 注意：这是一个特殊的Effect，生命周期由投射物控制
    /// </summary>
    public partial class ProjectileEffectSpecHandler : AEffectHandler
    {
        public override void Cancel()
        {
        }

        public override void Execute()
        {
        }

        public override SpecExecutionContext GetContext()
        {
            throw new System.NotImplementedException();
        }

        public override SpecExecutionContext GetExecutionContext()
        {
            throw new System.NotImplementedException();
        }

        public override void OnCompleteHook()
        {
            throw new System.NotImplementedException();
        }

        public override void OnInitialHook(AbilitySystemComponent target)
        {
            throw new System.NotImplementedException();
        }

        public override void OnInitialize()
        {
            throw new System.NotImplementedException();
        }

        public override void OnPeriodicHook()
        {
            throw new System.NotImplementedException();
        }

        public override void Reset()
        {
            throw new System.NotImplementedException();
        }

        public override void Tick(float deltaTime)
        {
            throw new System.NotImplementedException();
        }
    }
}
