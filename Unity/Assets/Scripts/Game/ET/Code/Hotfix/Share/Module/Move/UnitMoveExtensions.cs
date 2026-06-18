using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET
{
    public static class UnitMoveExtensions
    {
        public static void AddMoveComponentByMode(this Unit unit)
        {
            unit.GetOrAddComponent<MoveRestrictionComponent>();

            if (global::ET.ModeDefine.Is2D)
            {
                unit.GetOrAddComponent<Move2DComponent>();
            }
            else
            {
                unit.GetOrAddComponent<MoveComponent>();
            }
        }

        public static bool IsMoveAllowed(this Unit unit)
        {
            return unit?.GetComponent<MoveRestrictionComponent>().IsMoveAllowed() != false;
        }

        public static UniTask<bool> MoveAlongPathAsync(this Unit unit, List<float3> path, float speed, int turnTime = 100)
        {
            if (unit == null || !unit.IsMoveAllowed())
            {
                return UniTask.FromResult(false);
            }

            unit.AddMoveComponentByMode();

            if (global::ET.ModeDefine.Is2D)
            {
                return unit.GetComponent<Move2DComponent>().MoveToAsync(path, speed, turnTime);
            }

            return unit.GetComponent<MoveComponent>().MoveToAsync(path, speed, turnTime);
        }

        public static bool ChangeMoveSpeed(this Unit unit, float speed)
        {
            if (global::ET.ModeDefine.Is2D)
            {
                Move2DComponent moveComponent = unit.GetComponent<Move2DComponent>();
                return moveComponent != null && moveComponent.ChangeSpeed(speed);
            }

            MoveComponent component = unit.GetComponent<MoveComponent>();
            return component != null && component.ChangeSpeed(speed);
        }

        public static bool IsMoveArrived(this Unit unit)
        {
            if (global::ET.ModeDefine.Is2D)
            {
                return unit.GetComponent<Move2DComponent>()?.IsArrived() ?? true;
            }

            return unit.GetComponent<MoveComponent>()?.IsArrived() ?? true;
        }

        public static bool FlashMoveTo(this Unit unit, float3 target)
        {
            if (unit == null || !unit.IsMoveAllowed())
            {
                return false;
            }

            unit.AddMoveComponentByMode();

            if (global::ET.ModeDefine.Is2D)
            {
                return unit.GetComponent<Move2DComponent>().FlashTo(target);
            }

            return unit.GetComponent<MoveComponent>().FlashTo(target);
        }

        public static void StopMove(this Unit unit, bool ret)
        {
            if (global::ET.ModeDefine.Is2D)
            {
                unit.GetComponent<Move2DComponent>()?.Stop(ret);
                return;
            }

            unit.GetComponent<MoveComponent>()?.Stop(ret);
        }

        public static bool TryCreateMoveInfo(this Unit unit, out MoveInfo moveInfo)
        {
            moveInfo = null;

            if (global::ET.ModeDefine.Is2D)
            {
                Move2DComponent moveComponent = unit.GetComponent<Move2DComponent>();
                if (moveComponent == null || moveComponent.IsArrived())
                {
                    return false;
                }

                moveInfo = MoveInfo.Create();
                moveComponent.GetRemainingPath(moveInfo.Points);
                return true;
            }

            MoveComponent component = unit.GetComponent<MoveComponent>();
            if (component == null || component.IsArrived())
            {
                return false;
            }

            moveInfo = MoveInfo.Create();
            component.GetRemainingPath(moveInfo.Points);
            return true;
        }
    }
}
