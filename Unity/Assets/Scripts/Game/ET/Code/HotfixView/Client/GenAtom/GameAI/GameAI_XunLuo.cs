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
                return;
            }

            float speed = attributeComponent.GetValue(NumericType.Speed);
            if (speed <= 0f)
            {
                return;
            }

            while (!token.IsCancellationRequested)
            {
                if (!aiComponent.TryGetRandomPatrolTargetInScreen(aiConfig, out float3 nextTarget))
                {
                    return;
                }

                using ListComponent<float3> path = ListComponent<float3>.Create();
                path.Add(unit.Position);
                path.Add(nextTarget);

                bool arrived = await unit.MoveAlongPathAsync(path, speed);
                if (!arrived && token.IsCancellationRequested)
                {
                    return;
                }

                aiComponent.MarkPatrolIdle(aiConfig);
                return;
            }
        }
    }
}
