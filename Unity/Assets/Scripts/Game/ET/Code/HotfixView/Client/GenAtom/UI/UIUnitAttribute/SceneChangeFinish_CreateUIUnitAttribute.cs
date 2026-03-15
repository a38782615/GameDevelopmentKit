using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_CreateUIUnitAttribute : AEvent<Scene, SceneChangeFinish>
    {
        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {
            UIComponent uiComponent = scene.GetComponent<UIComponent>();
            uiComponent.RemoveComponent<UIFormUnitAttributeComponent>();
            await uiComponent.AddUIFormComponentAsync<UIFormUnitAttributeComponent>(UGFUIFormId.UIUnitAttribute);
        }
    }
}
