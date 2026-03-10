using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class AfterCreateClientScene_AddComponent : AEvent<Scene, AfterCreateClientScene>
    {
        protected override async UniTask Run(Scene scene, AfterCreateClientScene args)
        {
            scene.AddComponent<UIComponent>();
            // scene.AddComponent<GFEntityComponent>();
            await UniTask.CompletedTask;
        }
    }
}