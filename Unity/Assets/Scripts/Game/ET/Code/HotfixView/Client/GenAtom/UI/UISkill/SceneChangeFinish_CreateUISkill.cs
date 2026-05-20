using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_CreateUISkill : AEvent<Scene, SceneChangeFinish>
    {
        private const string UIMainAssetName = "Assets/Res/UI/UIForm/GenAtom/UIMain.prefab";
        private const string DefaultUIGroupName = "Default";

        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {
            UIComponent uiComponent = scene.GetComponent<UIComponent>();
            uiComponent.RemoveComponent<UIFormSkillComponent>();
            uiComponent.RemoveComponent<UIFormMainComponent>();

            UIFormMainComponent uiFormMain = uiComponent.AddComponent<UIFormMainComponent>();
            await uiFormMain.OpenUIFormAsync(
                UIMainAssetName,
                DefaultUIGroupName,
                Constant.AssetPriority.UIFormAsset,
                false);
        }
    }
}
