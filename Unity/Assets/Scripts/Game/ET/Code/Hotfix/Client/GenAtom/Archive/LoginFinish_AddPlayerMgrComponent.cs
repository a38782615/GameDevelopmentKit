using Cysharp.Threading.Tasks;

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class LoginFinish_AddPlayerMgrComponent : AEvent<Scene, LoginFinish>
    {
        protected override async UniTask Run(Scene scene, LoginFinish args)
        {
            if (scene.GetComponent<ArchiveMgrComponent>() == null)
            {
                scene.AddComponent<ArchiveMgrComponent>();
            }

            PlayerMgrComponent playerMgrComponent = scene.GetComponent<PlayerMgrComponent>();
            if (playerMgrComponent == null)
            {
                playerMgrComponent = scene.AddComponent<PlayerMgrComponent>();
            }

            await playerMgrComponent.LoadPlayerData();
        }
    }
}
