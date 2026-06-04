using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_CreateUIMain : AEvent<Scene, SceneChangeFinish>
    {
        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {
            if (SceneChangeHelper.IsSceneName(scene.Name, Tables.Instance.DTGameConfig.SceneMain))
            {
                UIComponent uiComponent = scene.GetComponent<UIComponent>();
                uiComponent.RemoveComponent<UIFormSkill>();
                uiComponent.RemoveComponent<UIFormMain>();

                await uiComponent.AddUIFormComponentAsync<UIFormMain>(UGFUIFormId.UIMain);
            }
        }
    }
}
