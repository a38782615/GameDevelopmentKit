using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangeRotation_SyncGameObjectRotation : AEvent<Scene, ChangeRotation>
    {
        protected override async UniTask Run(Scene scene, ChangeRotation args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }
            Transform transform = gameObjectComponent.GameObject.transform;
            SyncTransform(unit, transform);
            await UniTask.CompletedTask;
        }

        public static void SyncTransform(Unit unit, Transform transform)
        {
            if (global::ET.ModeDefine.Is2D)
            {
                Vector3 localScale = transform.localScale;
                float absScaleX = Mathf.Abs(localScale.x);
                if (absScaleX < 0.0001f)
                {
                    absScaleX = 1f;
                }

                float forwardX = unit.Forward.x;
                if (Mathf.Abs(forwardX) > 0.01f)
                {
                    localScale.x = forwardX < 0f ? absScaleX : -absScaleX;
                    transform.localScale = localScale;
                }
                return;
            }

            transform.rotation = unit.Rotation;
        }
    }
}
