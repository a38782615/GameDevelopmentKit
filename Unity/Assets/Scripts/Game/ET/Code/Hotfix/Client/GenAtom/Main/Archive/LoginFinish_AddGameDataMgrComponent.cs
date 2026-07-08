using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class LoginFinish_AddGameDataMgrComponent : AEvent<Scene, LoginFinish>
    {
        protected override async UniTask Run(Scene scene, LoginFinish args)
        {
            scene.GetOrAddComponent<ArchiveMgrComponent>();
            GameDataMgrComponent gameDataMgrComponent = scene.GetOrAddComponent<GameDataMgrComponent>();

            await gameDataMgrComponent.LoadAllData();
        }
    }
}
