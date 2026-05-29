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

            var skillUnit = unit.AddComponent<SkillUnit>();
            var config = unit.Config();

            var entiyId = config.EntityId;
            UGFEntity a = await scene.GetComponent<GFEntityComponent>().AddGFEntityChildAsync<CommonUGFEntity>(entiyId);

            
            GameObject viewGameObject = a.CachedTransform.gameObject;
            var gameObjectComponent = unit.GetOrAddComponent<GameObjectComponent>();
            gameObjectComponent.GameObject = viewGameObject;
            gameObjectComponent.Transform.position = unit.Position;
            ChangeRotation_SyncGameObjectRotation.SyncTransform(unit, gameObjectComponent.Transform);

            AbilitySystemComponent asc = skillUnit?.ASC.As();
            if (asc != null)
            {
                asc.SetOwnerObject(viewGameObject);
                SkillHudManager.GetOrCreate().RegisterUnit(
                    asc.InstanceId,
                    viewGameObject,
                    (UnitType)unit.Config().Type,
                    asc.Attributes?.GetCurrentValue(global::ET.NumericType.Hp) ?? 0f,
                    asc.Attributes?.GetCurrentValue(global::ET.NumericType.MaxHp) ?? 0f);
                SkelenAnimationComponent skelenAnimationComponent = unit.GetOrAddComponent<SkelenAnimationComponent>();
                skelenAnimationComponent.Bind();

                UnitMoveRestrictionComponent moveRestrictionComponent = unit.GetOrAddComponent<UnitMoveRestrictionComponent>();
                moveRestrictionComponent.Bind();
            }
            await UniTask.CompletedTask;
        }

    }
}
