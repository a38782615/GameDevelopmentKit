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
            unit.GetOrAddComponent<EntityBody>();
            // Unit View层
            // 这里资源需要卸载，Demo就不搞了
            //GameObject unitGo = await scene.Root().GetComponent<UGFComponent>().LoadAssetAsync<GameObject>(AssetUtility.GetPrefabAsset("Skeleton/Skeleton"));

            var skillUnit = unit.AddComponent<SkillUnit>();
            
            GFEntityHeadItem headEntity = unit.AddComponent<GFEntityHeadItem>();
            var config = unit.Config();
            await headEntity.ShowEntityAsync(config.EntityId);

            GameObject viewGameObject = headEntity.CachedTransform.gameObject;
            var gameObjectComponent = unit.GetOrAddComponent<GameObjectComponent>();
            gameObjectComponent.GameObject = viewGameObject;
            gameObjectComponent.Transform.position = unit.Position;
            ChangeRotation_SyncGameObjectRotation.SyncTransform(unit, gameObjectComponent.Transform);
            
            await headEntity.SetHeadIconAsync(unit);

            AbilitySystemComponent asc = skillUnit?.ASC.As();
            if (asc != null)
            {
                asc.SetOwnerObject(viewGameObject);
                SkillHudManager.GetOrCreate().RegisterUnit(
                    asc.InstanceId,
                    viewGameObject,
                    (UnitType)unit.Config().Type,
                    asc.Attributes?.GetValue(global::ET.NumericType.Hp) ?? 0f,
                    asc.Attributes?.GetValue(global::ET.NumericType.MaxHp) ?? 0f);
                    
                AnimationManagerComponent animationManagerComponent = unit.GetOrAddComponent<AnimationManagerComponent>();
                animationManagerComponent.Bind();

                UnitMoveRestrictionComponent moveRestrictionComponent = unit.GetOrAddComponent<UnitMoveRestrictionComponent>();
                moveRestrictionComponent.Bind();
            }
            await UniTask.CompletedTask;
        }

    }
}
