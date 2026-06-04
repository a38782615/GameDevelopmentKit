using System;
using Cysharp.Threading.Tasks;
using Game;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class SceneChangeStart_AddComponent : AEvent<Scene, SceneChangeStart>
    {
        protected override async UniTask Run(Scene currentScene, SceneChangeStart args)
        {
            try
            {
                await UGFComponent.Instance.UnloadAllScenesAsync();
                await UGFComponent.Instance.LoadSceneAsync(AssetUtility.GetSceneAsset(currentScene.Name));
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
