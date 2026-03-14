using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_CreateUISkill : AEvent<Scene, SceneChangeFinish>
    {
        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {
            UIComponent uiComponent = scene.GetComponent<UIComponent>();
            uiComponent.RemoveComponent<UIFormSkillComponent>();

            await scene.GetComponent<UIComponent>().AddUIFormComponentAsync<UIFormSkillComponent>(UGFUIFormId.UISkill);
        }
    }
}
