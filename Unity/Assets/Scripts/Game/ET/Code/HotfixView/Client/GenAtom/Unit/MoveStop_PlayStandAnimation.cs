using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(AnimationManagerComponent))]

    public class MoveStop_PlayStandAnimation : AEvent<Scene, MoveStop>
    {
        protected override async UniTask Run(Scene scene, MoveStop args)
        {
            _ = scene;
            Unit unit = args.Unit;
            if (unit == null)
            {
                return;
            }

            AnimationManagerComponent animationComponent = unit.GetComponent<AnimationManagerComponent>();
            if (animationComponent == null)
            {
                return;
            }

            animationComponent.PlayStandAnimation();
            await UniTask.CompletedTask;
        }
    }
}
