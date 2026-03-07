using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(TaskSpec))]
    public class SearchTargetTaskSpec : Entity, IAwake
    {
        [StaticField]
        public static bool DebugDraw = true;

        [StaticField]
        public static float DebugDrawDuration = 2f;

        [StaticField]
        public static Color DebugDrawColor = Color.green;
    }
}
