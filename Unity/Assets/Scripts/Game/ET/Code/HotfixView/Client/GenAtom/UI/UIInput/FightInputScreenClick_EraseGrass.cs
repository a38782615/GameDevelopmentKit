using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class FightInputScreenClick_EraseGrass : AEvent<Scene, FightInputScreenClick>
    {
        protected override async UniTask Run(Scene scene, FightInputScreenClick args)
        {
            if (scene == null || scene.IsDisposed || scene.Name != "Map2d")
            {
                await UniTask.CompletedTask;
                return;
            }

            GenMap genMap = scene.GetComponent<GenMap>();
            Camera camera = GameEntry.Camera?.CurrentSceneCamera;
            if (genMap == null || genMap.IsDisposed || camera == null)
            {
                await UniTask.CompletedTask;
                return;
            }

            genMap.TryEraseGrassAtScreenPoint(camera, new Vector2(args.ScreenPosition.x, args.ScreenPosition.y));
            await UniTask.CompletedTask;
        }
    }
}
