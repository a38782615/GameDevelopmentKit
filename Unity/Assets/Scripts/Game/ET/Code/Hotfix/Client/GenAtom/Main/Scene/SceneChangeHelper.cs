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

            // 可以订阅这个事件中创建Loading界面
            EventSystem.Instance.Publish(root, new SceneChangeStart());
            // 等待CreateMyUnit的消息
            Wait_CreateMyUnit waitCreateMyUnit = await root.GetComponent<ObjectWait>().Wait<Wait_CreateMyUnit>();
            M2C_CreateMyUnit m2CCreateMyUnit = waitCreateMyUnit.Message;
            Unit unit = UnitFactory.Create(currentScene, m2CCreateMyUnit.Unit);
            unitComponent.Add(unit);
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

            // 可以订阅这个事件中创建Loading界面
            EventSystem.Instance.Publish(root, new SceneChangeStart());
            CreateLocalUnitsFromTables(root, currentScene);
            EventSystem.Instance.Publish(currentScene, new SceneChangeFinish());
            root.GetComponent<ObjectWait>().Notify(new Wait_SceneChangeFinish());

            await UniTask.CompletedTask;
        }

        private static void CreateLocalUnitsFromTables(Scene root, Scene currentScene)
        {
            var dTHero = Tables.Instance.DTHero;
            for (int i = 0; i < dTHero.DataList.Count; i++)
            {
                var hero = dTHero.DataList[i];
                UnitInfo unitInfo = CreateHeroUnitInfo(hero, i);
                Unit unit = UnitFactory.Create(currentScene, unitInfo);
            }
        }

        private static UnitInfo CreateHeroUnitInfo(DRHero hero, int index)
        {
            UnitInfo unitInfo = UnitInfo.Create();
            unitInfo.UnitId = IdGenerater.Instance.GenerateInstanceId();
            unitInfo.ConfigId = hero.UnitConfigId;
            unitInfo.Type = hero.UnitConfigId_Ref.Type;
            unitInfo.Position = GetLocalUnitPosition((UnitType)hero.UnitConfigId_Ref.Type, 0);
            unitInfo.Forward = GetLocalUnitForward((UnitType)hero.UnitConfigId_Ref.Type);
            return unitInfo;
        }

        private static float3 GetLocalUnitPosition(UnitType unitType, int index)
        {
            return unitType switch
            {
                UnitType.Player => new float3(-6f + index * 2.5f, 0f, index * 1.5f),
                UnitType.Monster => new float3(6f + index * 2.5f, 0f, index * 1.5f),
                _ => new float3(index * 2.5f, 0f, -4f),
            };
        }

        private static float3 GetLocalUnitForward(UnitType unitType)
        {
            return unitType switch
            {
                UnitType.Player => new float3(1f, 0f, 0f),
                UnitType.Monster => new float3(-1f, 0f, 0f),
                _ => new float3(0f, 0f, 0f),
            };
        }
    }
}