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

            UIFormSkillComponent uiFormSkill = uiComponent.AddComponent<UIFormSkillComponent>();
            await uiFormSkill.OpenUIFormAsync(
                AssetUtility.GetUIFormAsset("GenAtom/UISkill"),
                "Pop",
                Constant.AssetPriority.UIFormAsset,
                false);
        }
    }
}
