using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(SkelenAnimationComponent))]
    public class SkillAnimationPlay_PlayUnitAnimation : AEvent<Scene, SkillAnimationPlay>
    {
        protected override async UniTask Run(Scene scene, SkillAnimationPlay args)
        {
            Unit unit = args.Unit;
            if (unit == null || string.IsNullOrEmpty(args.AnimationName))
            {
                await UniTask.CompletedTask;
                return;
            }

            SkelenAnimationComponent animationComponent = unit.GetComponent<SkelenAnimationComponent>();
            if (animationComponent == null)
            {
                animationComponent = unit.AddComponent<SkelenAnimationComponent>();
            }

            animationComponent.PlayAnimation(args.AnimationName, args.Loop);
            await UniTask.CompletedTask;
        }
    }
}
