using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async UniTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            SkillHudManager.Instance?.ClearSceneHud();
            scene.AddComponent<UIComponent>();
            scene.AddComponent<GFEntityComponent>();
            scene.AddComponent<GameplayCueManager>();
            scene.AddComponent<FightInputComponent>();
            await UniTask.CompletedTask;
        }
    }
}
