using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class UnitDeath_PlayDeathPresentationAndRemove : AEvent<Scene, UnitDeath>
    {
        private const string DeathAnimationName = "Death";
        // Keep a brief death presentation while removing defeated units promptly.
        private const int DefaultDeathRemoveDelayMs = 120;
        private const int MinDeathRemoveDelayMs = 120;
        private const int MaxDeathRemoveDelayMs = 3000;

        protected override async UniTask Run(Scene scene, UnitDeath args)
        {
            Unit unit = args.Unit;
            if (scene == null || scene.IsDisposed || unit == null || unit.IsDisposed)
            {
                return;
            }

            long unitId = unit.Id;
            AbilitySystemComponent asc = unit.GetComponent<SkillUnit>()?.ASC.As();
            long ascInstanceId = asc?.InstanceId ?? 0;

            unit.StopMove(false);
            unit.RemoveComponent<EntityBody>();

            AnimationManagerComponent animationManager = unit.GetOrAddComponent<AnimationManagerComponent>();
            animationManager.PlayAnimation(DeathAnimationName, false);
            int delayMs = animationManager.GetAnimationLengthMs(DeathAnimationName, string.Empty, DefaultDeathRemoveDelayMs);
            delayMs = Mathf.Clamp(delayMs, MinDeathRemoveDelayMs, MaxDeathRemoveDelayMs);
            SkillDiagFileLogger.Log($"[Animation] Play name={DeathAnimationName} asc={ascInstanceId} unit={unitId} removeDelayMs={delayMs}");

            TimerComponent timerComponent = scene.Root()?.GetComponent<TimerComponent>();
            if (timerComponent != null)
            {
                await timerComponent.WaitAsync(delayMs);
            }
            else
            {
                await UniTask.Delay(delayMs);
            }

            if (scene.IsDisposed || unit.IsDisposed)
            {
                return;
            }

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent?.Get(unitId) != unit)
            {
                return;
            }

            TryShowStageDropItem(scene, unit);
            bool isMonster = unit.Type() == UnitType.Monster;
            SkillDiagFileLogger.Log($"[Death] RemoveUnit asc={ascInstanceId} unit={unitId}");
            unitComponent.Remove(unitId);
            if (isMonster)
            {
                await scene.GetComponent<MapGenComponent>().TryGrantVictoryReward();
            }
        }

        private static void TryShowStageDropItem(Scene scene, Unit unit)
        {
            if (scene == null || scene.IsDisposed || unit == null || unit.Type() != UnitType.Monster)
            {
                return;
            }

            MapGenComponent mapGenComponent = scene.GetComponent<MapGenComponent>();
            DRStages stageConfig = mapGenComponent?.GetStageConfig();
            if (stageConfig?.DropIds == null || stageConfig.DropIds.Length == 0)
            {
                return;
            }

            RanDrawComponent ranDrawComponent = scene.GetOrAddComponent<RanDrawComponent>();
            int dropId = ranDrawComponent.GetDropItem(stageConfig.DropIds);
            if (dropId <= 0)
            {
                return;
            }

            ShowDropItemAsync(scene, dropId, GetDropPosition(unit)).Forget();
        }

        private static async UniTask ShowDropItemAsync(Scene scene, int dropId, Vector3 position)
        {
            if (scene == null || scene.IsDisposed)
            {
                return;
            }

            GFEntityComponent gfEntityComponent = scene.GetComponent<GFEntityComponent>();
            if (gfEntityComponent == null || gfEntityComponent.IsDisposed)
            {
                return;
            }

            try
            {
                await gfEntityComponent.AddGFEntityChildAsync<GFEntityDropItem, Vector3>(UGFEntityId.Item10001, position);

                var inventoryC = scene.Root().GetInventoryDataComponent();
                inventoryC.AddDrop(UGFEntityId.Item10001);
            }
            catch (System.Exception e)
            {
                Log.Error("[DropItem] Show drop item failed. dropId={0} entityId={1} error={2}"
                        .Fmt(dropId, UGFEntityId.Item10001, e));
            }
        }

        private static Vector3 GetDropPosition(Unit unit)
        {
            var position = unit.Position;
            return new Vector3(position.x, position.y, position.z);
        }
    }
}
