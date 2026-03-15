using Cysharp.Threading.Tasks;
using Game;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class FightInputScreenClick_MovePlayerUnit : AEvent<Scene, FightInputScreenClick>
    {
        protected override async UniTask Run(Scene scene, FightInputScreenClick args)
        {
            Unit unit = UnitHelper.GetMyUnitFromCurrentScene(scene);
            if (unit == null || unit.IsDisposed)
            {
#if UNITY_EDITOR
                Log.Warning("[FightInput] MovePlayer skipped: player unit is null.");
#endif
                await UniTask.CompletedTask;
                return;
            }

            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            if (numericComponent == null)
            {
#if UNITY_EDITOR
                Log.Warning(
                    $"[FightInput] MovePlayer skipped: numeric={numericComponent != null} unit={unit.Id}");
#endif
                await UniTask.CompletedTask;
                return;
            }

            if (!TryGetWorldPosition(unit, args.ScreenPosition, out float3 targetPosition))
            {
#if UNITY_EDITOR
                Log.Warning(
                    $"[FightInput] MovePlayer skipped: screen to world failed unit={unit.Id} screen=({args.ScreenPosition.x:0.##},{args.ScreenPosition.y:0.##})");
#endif
                await UniTask.CompletedTask;
                return;
            }

            if (math.distancesq(unit.Position.ToPlanar(), targetPosition.ToPlanar()) < 0.0001f)
            {
#if UNITY_EDITOR
                Log.Info(
                    $"[FightInput] MovePlayer ignored: target too close unit={unit.Id} from=({unit.Position.x:0.##},{unit.Position.y:0.##}) to=({targetPosition.x:0.##},{targetPosition.y:0.##})");
#endif
                await UniTask.CompletedTask;
                return;
            }

            float speed = numericComponent.GetAsFloat(NumericType.Speed);
            if (speed <= 0f)
            {
#if UNITY_EDITOR
                Log.Warning(
                    $"[FightInput] MovePlayer skipped: invalid speed={speed:0.##} unit={unit.Id}");
#endif
                await UniTask.CompletedTask;
                return;
            }

#if UNITY_EDITOR
            Log.Info(
                $"[FightInput] MovePlayer start unit={unit.Id} from=({unit.Position.x:0.##},{unit.Position.y:0.##}) to=({targetPosition.x:0.##},{targetPosition.y:0.##}) speed={speed:0.##} screen=({args.ScreenPosition.x:0.##},{args.ScreenPosition.y:0.##})");
#endif
            using ListComponent<float3> path = ListComponent<float3>.Create();
            path.Add(unit.Position);
            path.Add(targetPosition);

            await unit.MoveAlongPathAsync(path, speed);
        }

        private static bool TryGetWorldPosition(Unit unit, float2 screenPosition, out float3 worldPosition)
        {
            worldPosition = default;

            Camera camera = GameEntry.Camera?.CurrentSceneCamera;
            if (camera == null)
            {
                return false;
            }

            Ray ray = camera.ScreenPointToRay(new Vector3(screenPosition.x, screenPosition.y, 0f));
            int mapMask = LayerMask.GetMask("Map");
            if (mapMask != 0 && Physics.Raycast(ray, out RaycastHit hit, 1000f, mapMask))
            {
                worldPosition = new float3(hit.point.x, hit.point.y, hit.point.z);
                return true;
            }

            Plane primaryPlane = global::ET.ModeDefine.Is2D
                ? new Plane(Vector3.forward, new Vector3(0f, 0f, unit.Position.z))
                : new Plane(Vector3.up, new Vector3(0f, unit.Position.y, 0f));
            if (TryProjectToPlane(ray, primaryPlane, out worldPosition))
            {
                return true;
            }

            Plane fallbackPlane = global::ET.ModeDefine.Is2D
                ? new Plane(Vector3.up, new Vector3(0f, unit.Position.y, 0f))
                : new Plane(Vector3.forward, new Vector3(0f, 0f, unit.Position.z));
            return TryProjectToPlane(ray, fallbackPlane, out worldPosition);
        }

        private static bool TryProjectToPlane(Ray ray, Plane plane, out float3 worldPosition)
        {
            worldPosition = default;
            if (!plane.Raycast(ray, out float distance))
            {
                return false;
            }

            Vector3 point = ray.GetPoint(distance);
            worldPosition = new float3(point.x, point.y, point.z);
            return true;
        }
    }
}
