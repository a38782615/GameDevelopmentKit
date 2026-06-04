using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_CreateUIFight : AEvent<Scene, SceneChangeFinish>
    {
        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {

            if (SceneChangeHelper.IsSceneName(scene.Name, Tables.Instance.DTGameConfig.SceneMapFight))
            {
                UIComponent uiComponent = scene.GetComponent<UIComponent>();
                uiComponent.RemoveComponent<UIFormFight>();

                await uiComponent.AddUIFormComponentAsync<UIFormFight>(UGFUIFormId.UIFormFight);
            }
        }
    }
}
