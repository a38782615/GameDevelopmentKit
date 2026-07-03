using Cysharp.Threading.Tasks;
namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class GoMap_StartSceneChange : AEvent<Scene, GoScene>
    {
        protected override async UniTask Run(Scene scene, GoScene args)
        {
            // await SceneChangeHelper.SceneChangeTo2(scene, "Map2d", 1000000000000000000);
            await SceneChangeHelper.SceneChangeTo2(scene, SceneChangeHelper.GetSceneName(args.SceneId), args);
            await UniTask.CompletedTask;
        }
    }
}