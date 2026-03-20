using System.Diagnostics;

namespace ET.Client
{
    [EnableClass]
    public static class SkillDiagFileLogger
    {
        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            UnityEngine.Debug.LogWarning(message);
#endif
        }
    }
}
