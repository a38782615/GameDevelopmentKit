using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class NumericChangeEvent_NotifyAbilitySystem : AEvent<Scene, NumbericChange>
    {
        protected override async UniTask Run(Scene scene, NumbericChange args)
        {
            AbilitySystemComponent asc = args.Unit?.GetComponent<SkillUnit>()?.ASC.As();
            if (asc != null)
            {
                asc.HandleAttributeChanged(args.NumericType, args.Old, args.New);
            }

            await UniTask.CompletedTask;
        }
    }
}
