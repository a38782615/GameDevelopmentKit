using Cysharp.Threading.Tasks;
#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ET.Client
{
    [Event(SceneType.GenAtom)]
    public class AppStartInitFinish_CreateLoginUI : AEvent<Scene, AppStartInitFinish>
    {
        protected override async UniTask Run(Scene scene, AppStartInitFinish args)
        {
            await scene.GetComponent<UIComponent>().AddUIFormComponentAsync<UIFormLoginComponent>(UGFUIFormId.UILogin);
            // #if UNITY_EDITOR
            //             if (Application.isPlaying)
            //             {
            //                 Log.Info("[UISkill][Editor] 自动触发 LoginFinish 进入本地技能场景");
            //                 await EventSystem.Instance.PublishAsync(scene, new LoginFinish());
            //             }
            // #endif
        }
    }
}
