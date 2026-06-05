using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(AnimationManagerComponent))]
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

            AnimationManagerComponent animationComponent = unit.GetComponent<AnimationManagerComponent>();
            if (animationComponent == null)
            {
                animationComponent = unit.AddComponent<AnimationManagerComponent>();
            }

            animationComponent.PlayAnimation(args.AnimationName, args.Loop, args.AnimationComponentPath);
            await UniTask.CompletedTask;
        }
    }
}
