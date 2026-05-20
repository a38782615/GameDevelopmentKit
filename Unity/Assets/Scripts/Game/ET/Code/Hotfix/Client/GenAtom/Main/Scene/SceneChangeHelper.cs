using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    public static partial class SceneChangeHelper
    {
        // 场景切换协程
        public static async UniTask SceneChangeTo(Scene root, string sceneName, long sceneInstanceId)
        {
            root.RemoveComponent<AIComponent>();

            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的
            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
            UnitComponent unitComponent = currentScene.AddComponent<UnitComponent>();
            currentScene.AddComponent<BodyCheckComponent>();
            currentScene.AddComponent<MovementSimulationComponent>();

            // 等待场景资源切换完成，避免后续运行时对象落到常驻管理场景。
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeStart());
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeBeforeLoadUnit());
            // 等待CreateMyUnit的消息
            Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            unitComponent.Add(unit);
            await EventSystem.Instance.PublishAsync(currentScene, new AfterUnitCreate() { Unit = unit });
            root.RemoveComponent<AIComponent>();

            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }

        public static async UniTask SceneChangeTo2(Scene root, string sceneName, long sceneInstanceId)
        {
            root.RemoveComponent<AIComponent>();

            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的

            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
            currentScene.AddComponent<UnitComponent>();
            currentScene.AddComponent<BodyCheckComponent>();
            currentScene.AddComponent<MovementSimulationComponent>();

            // 等待场景资源切换完成，避免后续运行时对象落到常驻管理场景。
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeStart());
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeBeforeLoadUnit());

            //创建units
            await CreateLocalUnitsFromTables(root, currentScene);

            //加载个ui
            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }

        public static async UniTask SceneChangeToUIMap(Scene root, string sceneName, long sceneInstanceId)
        {
            root.RemoveComponent<AIComponent>();

            CurrentScenesComponent currentScenesComponent = root.GetComponent<CurrentScenesComponent>();
            currentScenesComponent.Scene?.Dispose(); // 删除之前的CurrentScene，创建新的

            Scene currentScene = CurrentSceneFactory.Create(sceneInstanceId, sceneName, currentScenesComponent);
            currentScene.AddComponent<UnitComponent>();

            // 等待场景资源切换完成，避免后续运行时对象落到常驻管理场景。
            await EventSystem.Instance.PublishAsync(currentScene, new SceneChangeStart());

            //加载个ui
            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
            // 通知等待场景切换的协程
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());
        }

        private static async UniTask CreateLocalUnitsFromTables(Scene root, Scene currentScene)
        {
            var heros = Tables.Instance.DTHero;
            var unis1 = new UniTask[heros.DataList.Count];
            for (int i = 0; i < heros.DataList.Count; i++)
            {
                var config = heros.DataList[i];
                UnitInfo unitInfo = CreateUnitInfo(config, i);
                Unit unit = UnitFactory.Create(currentScene, unitInfo);
                if (i == 0)
                {
                    root.GetComponent<PlayerComponent>().MyId = unitInfo.UnitId;
                }
                var t = EventSystem.Instance.PublishAsync(currentScene, new AfterUnitCreate() { Unit = unit });
                unis1[i] = t;
            }
            await UniTask.WhenAll(unis1);

            var configs = Tables.Instance.DTMonster;
            var unis = new UniTask[configs.DataList.Count];
            for (int i = 0; i < configs.DataList.Count; i++)
            {
                var config = configs.DataList[i];
                UnitInfo unitInfo = CreateUnitInfo(config, i);
                Unit unit = UnitFactory.Create(currentScene, unitInfo);

                var t = EventSystem.Instance.PublishAsync(currentScene, new AfterUnitCreate() { Unit = unit });
                unis[i] = t;
            }

            await UniTask.WhenAll(unis);
        }

        private static UnitInfo CreateUnitInfo(DRHero config, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;

            unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, index);
            unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
            return unitInfo;
        }
        private static UnitInfo CreateUnitInfo(DRMonster config, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = config.Id;
            unitInfo.Type = config.UnitConfigId_Ref.Type;
            unitInfo.ConfigId = config.UnitConfigId;

            unitInfo.Position = GetLocalUnitPosition((UnitType)unitInfo.Type, index);
            unitInfo.Forward = GetLocalUnitForward((UnitType)unitInfo.Type);
            return unitInfo;
        }

        private static float3 GetLocalUnitPosition(UnitType unitType, int index)
        {
            return unitType switch
            {
                UnitType.Player => new float2(-3f + index * 2.5f, index * 1.5f).ToModePosition(),
                UnitType.Monster => new float2(3f + index * 2.5f, index * 1.5f).ToModePosition(),
                _ => new float2(index * 2.5f, -4f).ToModePosition(),
            };
        }

        private static float3 GetLocalUnitForward(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Player => new float2(1f, 0f).ToModeDirection(),
                UnitType.Monster => new float2(-1f, 0f).ToModeDirection(),
                _ => float3.zero,
            };
        }
    }
}
