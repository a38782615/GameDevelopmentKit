using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterCreateCurrentScene_AddComponent : AEvent<Scene, AfterCreateCurrentScene>
    {
        protected override async UniTask Run(Scene scene, AfterCreateCurrentScene args)
        {
            scene.AddComponent<UIComponent>();
            scene.AddComponent<GFEntityComponent>();
            scene.AddComponent<UnitComponent>();
            if (SceneChangeHelper.IsSceneName(scene.Name, Tables.Instance.DTGameConfig.SceneMapFight))
            {
                scene.AddComponent<BodyCheckComponent>();
                scene.AddComponent<BattleTurnComponent>();
                scene.AddComponent<FightComponent>();
                // RVO
                // scene.AddComponent<MovementSimulationComponent>();
                
                SkillHudManager.Instance?.ClearSceneHud();
                scene.AddComponent<FightInputComponent>();
            }
            await UniTask.CompletedTask;
        }
    }
}
