using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    public static partial class SceneChangeHelper
    {
        // 场景切换协程
        public static async UniTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的
            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
            UnitComponent unitComponent = currentScene.AddComponent<UnitComponent>();
            currentScene.AddComponent<BodyCheckComponent>();
            currentScene.AddComponent<MovementSimulationComponent>();

            // 等待场景资源切换完成，避免后续运行时对象落到常驻管理场景。
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeStart()
            {
            });
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeBeforeLoadUnit());
            // 等待CreateMyUnit的消息
            Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            unitComponent.Add(unit);
            await EventSystem.Instance.PublishAsync(currentScene, new AfterUnitCreate() { Unit = unit });

            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish()
            {
            });
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }

        public static async UniTask SceneChangeTo2(Scene root, string sceneName, long sceneInstanceId)
        {
            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的

            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);

            // 等待场景资源切换完成，避免后续运行时对象落到常驻管理场景。
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeStart()
            {
            });
            // 生成地图
            // await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeBeforeLoadUnit());

            //加载个ui
            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish()
            {
            });
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }


        public static string GetSceneName(int sceneId)
        {
            var ret = Tables.Instance.DTScene.GetOrDefault(sceneId).CSName;
            return ret;
        }

        public static bool IsSceneName(string sceneName, int sceneId)
        {
            return sceneName == GetSceneName(sceneId);
        }
    }
}
