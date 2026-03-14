using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Server
{
    [Event(SceneType.Map)]
    public class ChangePosition_NotifyAOI: AEvent<Scene, ChangePosition>
    {
        protected override async UniTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            float2 oldPos = args.OldPos.ToPlanar();
            float2 newPos = unit.Position.ToPlanar();
            int oldCellX = (int)(oldPos.x * 1000) / AOIManagerComponent.CellSize;
            int oldCellY = (int)(oldPos.y * 1000) / AOIManagerComponent.CellSize;
            int newCellX = (int)(newPos.x * 1000) / AOIManagerComponent.CellSize;
            int newCellY = (int)(newPos.y * 1000) / AOIManagerComponent.CellSize;
            if (oldCellX == newCellX && oldCellY == newCellY)
            {
                return;
            }

            AOIEntity aoiEntity = unit.GetComponent<AOIEntity>();
            if (aoiEntity == null)
            {
                return;
            }

            unit.Scene().GetComponent<AOIManagerComponent>().Move(aoiEntity, newCellX, newCellY);
            await UniTask.CompletedTask;
        }
    }
}
