using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class LoginFinish_AddGameDataMgrComponent : AEvent<Scene, LoginFinish>
    {
        protected override async UniTask Run(Scene scene, LoginFinish args)
        {
            if (scene.GetComponent<ArchiveMgrComponent>() == null)
            {
                scene.AddComponent<ArchiveMgrComponent>();
            }

            GameDataMgrComponent gameDataMgrComponent = scene.GetComponent<GameDataMgrComponent>();
            if (gameDataMgrComponent == null)
            {
                gameDataMgrComponent = scene.AddComponent<GameDataMgrComponent>();
            }

            await gameDataMgrComponent.LoadAllData();
        }
    }
}
