using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class LoginFinish_AddArchiveMgrComponent : AEvent<Scene, LoginFinish>
    {
        protected override async UniTask Run(Scene scene, LoginFinish args)
        {
            if (scene.GetComponent<ArchiveMgrComponent>() == null)
            {
                scene.AddComponent<ArchiveMgrComponent>();
            }

            await UniTask.CompletedTask;
        }
    }
}
