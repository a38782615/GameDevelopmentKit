using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeFinish_SceneChangeFinish : AEvent<Scene, SceneChangeFinish>
    {
        protected override async UniTask Run(Scene scene, SceneChangeFinish args)
        {
            if (SceneChangeHelper.IsSceneName(scene.Name, Tables.Instance.DTGameConfig.SceneMain))
            {
                UIComponent uiComponent = scene.GetComponent<UIComponent>();
                if (args.UI == UGFUIFormId.UIMain)
                {
                    await uiComponent.AddUIFormComponentAsync<UIFormMain>(args.UI);
                }
                else if (args.UI == UGFUIFormId.UIFormBag1)
                {
                    await uiComponent.AddUIFormComponentAsync<UIFormBag1>(args.UI);
                }
                else if( args.UI == UGFUIFormId.UIFormMap)
                {
                    await uiComponent.AddUIFormComponentAsync<UIFormMap>(args.UI);
                }
                else if (args.UI == UGFUIFormId.UIFormSkills)
                {
                    await uiComponent.AddUIFormComponentAsync<UIFormSkills>(args.UI);
                }
                else if (args.UI == UGFUIFormId.UIFormFight)
                {
                    await uiComponent.AddUIFormComponentAsync<UIFormFight>(args.UI);
                }
            }


            if (SceneChangeHelper.IsSceneName(scene.Name, Tables.Instance.DTGameConfig.SceneMapFight))
            {
                UIComponent uiComponent = scene.GetComponent<UIComponent>();
                await uiComponent.AddUIFormComponentAsync<UIFormFight>(UGFUIFormId.UIFormFight);
            }
        }
    }
}
