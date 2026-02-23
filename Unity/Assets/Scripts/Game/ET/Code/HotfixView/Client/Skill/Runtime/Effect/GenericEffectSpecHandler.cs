

namespace ET.Client
{
    /// <summary>
    /// 通用效果Spec - 完全依赖基类处理
    /// </summary>
    public partial class GenericEffectSpecHandler : AEffectHandler
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
