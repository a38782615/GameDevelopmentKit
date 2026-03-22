using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOfAttribute(typeof(ET.Client.SkelenAnimationComponent))]

    public class MoveStart_PlayMoveAnimation : AEvent<Scene, MoveStart>
    {
        protected override async UniTask Run(Scene scene, MoveStart args)
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
            animationComponent.PlayAnimation(animationComponent.MoveAnimationName, true);
            await UniTask.CompletedTask;
        }
    }
}
