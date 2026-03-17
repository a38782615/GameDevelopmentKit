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

            if (aiComponent.HasPendingPatrolIdle())
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

            global::ET.AttributeComponent attributeComponent = unit.GetComponent<global::ET.AttributeComponent>();
            if (attributeComponent == null)
            {
#if UNITY_EDITOR
                Log.Warning($"[GameAI] XunLuo skipped: attribute missing unit={unit.Id} config={unit.ConfigId}");
#endif
                return;
            }

            float speed = attributeComponent.GetCurrentValue(NumericType.Speed);
            if (speed <= 0f)
            {
#if UNITY_EDITOR
                SkillDiagFileLogger.Log($"[DiagGameAI] xunluo invalid-speed unit={unit.Id} config={unit.ConfigId} speed={speed:0.##}");
                Log.Warning($"[GameAI] XunLuo skipped: invalid speed={speed:0.##} unit={unit.Id} config={unit.ConfigId}");
#endif
                return;
            }

#if UNITY_EDITOR
            SkillDiagFileLogger.Log($"[DiagGameAI] xunluo execute-start unit={unit.Id} config={unit.ConfigId} speed={speed:0.##}");
#endif

            while (!token.IsCancellationRequested)
            {
                if (!aiComponent.TryGetRandomPatrolTargetInScreen(aiConfig, out float3 nextTarget))
                {
#if UNITY_EDITOR
                    SkillDiagFileLogger.Log($"[DiagGameAI] xunluo target-failed unit={unit.Id} config={unit.ConfigId}");
                    Log.Warning($"[GameAI] XunLuo skipped: target build failed unit={unit.Id} config={unit.ConfigId}");
#endif
                    return;
                }

                using ListComponent<float3> path = ListComponent<float3>.Create();
                path.Add(unit.Position);
                path.Add(nextTarget);

#if UNITY_EDITOR
                SkillDiagFileLogger.Log(
                    $"[DiagGameAI] xunluo move-start unit={unit.Id} config={unit.ConfigId} from=({unit.Position.x:0.##},{unit.Position.y:0.##},{unit.Position.z:0.##}) to=({nextTarget.x:0.##},{nextTarget.y:0.##},{nextTarget.z:0.##}) speed={speed:0.##}");
                Log.Info(
                    $"[GameAI] XunLuo local move unit={unit.Id} config={unit.ConfigId} from=({unit.Position.x:0.##},{unit.Position.y:0.##},{unit.Position.z:0.##}) to=({nextTarget.x:0.##},{nextTarget.y:0.##},{nextTarget.z:0.##}) speed={speed:0.##}");
#endif

                bool arrived = await unit.MoveAlongPathAsync(path, speed);
                if (!arrived && token.IsCancellationRequested)
                {
                    return;
                }

                aiComponent.MarkPatrolIdle(aiConfig);
#if UNITY_EDITOR
                SkillDiagFileLogger.Log(
                    $"[DiagGameAI] xunluo arrived unit={unit.Id} config={unit.ConfigId} idleMs={aiComponent.GetRemainingPatrolIdleMs()}");
                Log.Info(
                    $"[GameAI] XunLuo arrived unit={unit.Id} config={unit.ConfigId} idleMs={aiComponent.GetRemainingPatrolIdleMs()}");
#endif
                return;
            }
        }
    }
}
