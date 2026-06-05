using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(AnimationManagerComponent))]

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

            AbilitySystemComponent asc = unit.GetComponent<SkillUnit>()?.ASC.As();
            if (!asc.IsAlive())
            {
                return;
            }

            AnimationManagerComponent animationComponent = unit.GetComponent<AnimationManagerComponent>();
            if (animationComponent == null)
            {
                return;
            }

            animationComponent.PlayMoveAnimation();
            await UniTask.CompletedTask;
        }
    }
}
