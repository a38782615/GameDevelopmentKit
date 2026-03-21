using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
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

            SkelenAnimationComponent animationComponent = unit.GetComponent<SkelenAnimationComponent>();
            if (animationComponent == null || animationComponent.IsStunned)
            {
                return;
            }

            animationComponent.PlayAnimation(animationComponent.StandAnimationName, true);
            await UniTask.CompletedTask;
        }
    }
}
