using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeStart_ClearSkillHud : AEvent<Scene, SceneChangeStart>
    {
        protected override async UniTask Run(Scene currrent, SceneChangeStart args)
        {
            SkillHudManager.Instance?.ClearSceneHud();
            await UniTask.CompletedTask;
        }
    }
}
