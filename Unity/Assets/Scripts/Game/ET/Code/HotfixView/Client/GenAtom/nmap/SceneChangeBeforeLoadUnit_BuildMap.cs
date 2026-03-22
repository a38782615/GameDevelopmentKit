using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeBeforeLoadUnit_BuildMap : AEvent<Scene, SceneChangeBeforeLoadUnit>
    {
        protected override async UniTask Run(Scene scene, SceneChangeBeforeLoadUnit args)
        {
            if (scene == null || scene.Name != "Map2d")
            {
                await UniTask.CompletedTask;
                return;
            }

            GenMap genMap = scene.GetComponent<GenMap>();
            if (genMap == null)
            {
                genMap = scene.AddComponent<GenMap>();
            }

            genMap.Build();
            await UniTask.CompletedTask;
        }
    }
}
