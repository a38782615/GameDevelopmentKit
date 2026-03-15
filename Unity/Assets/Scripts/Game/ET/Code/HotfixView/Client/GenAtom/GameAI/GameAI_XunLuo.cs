using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Mathematics;

namespace ET.Client
{
    public class GameAI_XunLuo : AGameAIHandler
    {
        public override int Check(GameAIComponent aiComponent, DRGameAI aiConfig)
        {
            Unit unit = aiComponent?.GetOwnerUnit();
            if (unit == null)
            {
                return 1;
            }

            if (aiComponent.FindNearestTarget(aiConfig.GetAttackRange()) != null)
            {
                return 1;
            }

            return 0;
        }

        public override async UniTask Execute(GameAIComponent aiComponent, DRGameAI aiConfig, CancellationToken token)
        {
            Unit unit = aiComponent?.GetOwnerUnit();
            if (unit == null)
            {
                return;
            }

            NumericComponent numericComponent = unit.GetComponent<NumericComponent>();
            if (numericComponent == null)
            {
#if UNITY_EDITOR
                Log.Warning($"[GameAI] XunLuo skipped: numeric missing unit={unit.Id} config={unit.ConfigId}");
#endif
                return;
            }

            float speed = numericComponent.GetAsFloat(NumericType.Speed);
            if (speed <= 0f)
            {
#if UNITY_EDITOR
                Log.Warning($"[GameAI] XunLuo skipped: invalid speed={speed:0.##} unit={unit.Id} config={unit.ConfigId}");
#endif
                return;
            }

            XunLuoPathComponent pathComponent = unit.GetComponent<XunLuoPathComponent>();
            if (pathComponent == null)
            {
                pathComponent = unit.AddComponent<XunLuoPathComponent>();
            }

            while (!token.IsCancellationRequested)
            {
                float3 nextTarget = pathComponent.GetCurrent();

                using ListComponent<float3> path = ListComponent<float3>.Create();
                path.Add(unit.Position);
                path.Add(nextTarget);

#if UNITY_EDITOR
                Log.Info(
                    $"[GameAI] XunLuo local move unit={unit.Id} config={unit.ConfigId} from=({unit.Position.x:0.##},{unit.Position.y:0.##},{unit.Position.z:0.##}) to=({nextTarget.x:0.##},{nextTarget.y:0.##},{nextTarget.z:0.##}) speed={speed:0.##}");
#endif

                bool arrived = await unit.MoveAlongPathAsync(path, speed);
                if (!arrived && token.IsCancellationRequested)
                {
                    return;
                }

                pathComponent.MoveNext();
            }
        }
    }
}
