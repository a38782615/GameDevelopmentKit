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
            // UIFormLoginComponent uiFormLogin = await scene.GetComponent<UIComponent>().AddUIFormComponentAsync<UIFormLoginComponent>(UGFUIFormId.UILogin);
            // await uiFormLogin.WaitAllTestWidgetsLoadedAsync();

            await EventSystem.Instance.PublishAsync(scene, new LoginFinish());
        }
    }
}
