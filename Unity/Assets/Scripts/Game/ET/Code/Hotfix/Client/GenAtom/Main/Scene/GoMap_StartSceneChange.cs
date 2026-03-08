using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class GoMap_StartSceneChange : AEvent<Scene, GoMap2d>
    {
        protected override async UniTask Run(Scene scene, GoMap2d args)
        {
            await SceneChangeHelper.SceneChangeTo2(scene, "Map2d", 1000000000000000000);
            await UniTask.CompletedTask;
        }
    }
}