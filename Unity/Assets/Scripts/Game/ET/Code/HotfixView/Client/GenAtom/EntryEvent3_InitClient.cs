using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Main)]
    public class EntryEvent3_InitClient: AEvent<Scene, EntryEvent3>
    {
        protected override async UniTask Run(Scene root, EntryEvent3 args)
        {
            //Test
            root.AddComponent<TestComponent>();

            InitData();

            root.AddComponent<UGFComponent>();
            
            GlobalComponent globalComponent = root.AddComponent<GlobalComponent>();
            root.AddComponent<UIComponent>();
            root.AddComponent<PlayerComponent>();
            root.AddComponent<CurrentScenesComponent>();
            root.AddComponent<RanDrawComponent>();
            await SkillDataCenter.Instance.EnsureLoadedAndPreloadAsync();

            // 根据配置修改掉Main Fiber的SceneType
            SceneType sceneType = EnumHelper.FromString<SceneType>(globalComponent.AppType.ToString());
            root.SceneType = sceneType;

            await EventSystem.Instance.PublishAsync(root, new AppStartInitFinish());
        }
        
        private void InitData()
        {
            GameConst.DataPath = Application.persistentDataPath;
        }
    }
}
