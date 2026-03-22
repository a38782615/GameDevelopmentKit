using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangePosition_MarkBodyDirty : AEvent<Scene, ChangePosition>
    {
        protected override async UniTask Run(Scene scene, ChangePosition args)
        {
            EntityBody entityBody = args.Unit?.GetComponent<EntityBody>();
            scene?.GetComponent<BodyCheckComponent>()?.MarkDirty(entityBody);
            await UniTask.CompletedTask;
        }
    }
}
