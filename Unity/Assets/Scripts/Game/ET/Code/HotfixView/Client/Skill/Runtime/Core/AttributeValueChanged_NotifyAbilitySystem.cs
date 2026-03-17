using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AttributeValueChanged_NotifyAbilitySystem : AEvent<Scene, AttributeValueChanged>
    {
        protected override async UniTask Run(Scene scene, AttributeValueChanged args)
        {
            AbilitySystemComponent asc = args.Unit?.GetComponent<SkillUnit>()?.ASC.As();
            if (asc != null)
            {
                asc.HandleAttributeChanged(args.NumericType, args.OldValue, args.NewValue);
            }

            await UniTask.CompletedTask;
        }
    }
}
