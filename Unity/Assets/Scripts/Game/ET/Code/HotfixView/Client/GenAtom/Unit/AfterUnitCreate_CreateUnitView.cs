using Cysharp.Threading.Tasks;
using Game;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    [FriendOf(typeof(AbilitySystemComponent))]
    public class AfterUnitCreate_CreateUnitView : AEvent<Scene, AfterUnitCreate>
    {
        protected override async UniTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagSkillUnit] before AddComponent<SkillUnit> newGO={CountAnonymousRootObjects()} unit={unit.ConfigId}");
#endif
            var skillUnit = unit.AddComponent<SkillUnit>();
#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagSkillUnit] after AddComponent<SkillUnit> newGO={CountAnonymousRootObjects()} unit={unit.ConfigId}");
#endif
            // Unit View层
            // 这里资源需要卸载，Demo就不搞了
            // GameObject unitGo = await UGFComponent.Instance.LoadAssetAsync<GameObject>(AssetUtility.GetPrefabAsset("Skeleton/Skeleton"));

            // GameObject go = UnityEngine.Object.Instantiate(unitGo);
            // go.transform.position = unit.Position;
            // unit.AddComponent<GameObjectComponent>().GameObject = go;
            var config = unit.Config();
            var entiyId = config.EntityId;
            UGFEntity a = await scene.GetComponent<GFEntityComponent>().AddGFEntityChildAsync<CommonUGFEntity>(entiyId);
            GameObject viewGameObject = a.CachedTransform.gameObject;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                gameObjectComponent = unit.AddComponent<GameObjectComponent>();
            }

            gameObjectComponent.GameObject = viewGameObject;

            AbilitySystemComponent asc = skillUnit?.ASC.As();
            if (asc != null)
            {
                asc.Owner = viewGameObject;
            }

            unit.AddComponent<Collider2DComponent>().Bind(viewGameObject, asc);
            await UniTask.CompletedTask;
        }

#if UNITY_EDITOR
        private static int CountAnonymousRootObjects()
        {
            var rootGameObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            int count = 0;
            foreach (var gameObject in rootGameObjects)
            {
                if (gameObject != null && gameObject.name == "New Game Object")
                {
                    count++;
                }
            }

            return count;
        }
#endif
    }
}
