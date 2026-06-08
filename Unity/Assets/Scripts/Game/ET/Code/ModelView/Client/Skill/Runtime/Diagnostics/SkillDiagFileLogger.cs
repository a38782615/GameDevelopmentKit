using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ET.Client
{
    [EnableClass]
    public static class SkillDiagFileLogger
    {
#if UNITY_EDITOR
        [StaticField]
        private static readonly object SyncRoot = new object();
        [StaticField]
        private static string currentLogFilePath;
        [StaticField]
        private static bool wasPlaying;
        [StaticField]
        private static long battleLoadCompleteMs;
        [StaticField]
        private static bool hasBattleLoadComplete;
        [StaticField]
        private static string battleLoadContext;
        [StaticField]
        private static readonly Dictionary<string, long> abilityActivationMs = new Dictionary<string, long>();
#endif

        [Conditional("UNITY_EDITOR")]
        public static void Log(string message)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            lock (SyncRoot)
            {
                WriteLine(message);
            }
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void MarkBattleLoadComplete(string context)
        {
#if UNITY_EDITOR
            lock (SyncRoot)
            {
                battleLoadCompleteMs = GetRealtimeMs();
                hasBattleLoadComplete = true;
                battleLoadContext = context ?? string.Empty;
                abilityActivationMs.Clear();
                WriteLine($"[Timing] BattleLoadComplete context={battleLoadContext} realtimeMs={battleLoadCompleteMs}");
            }
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogAbilityActivated(string skillId, long casterUnitId, long casterAscId, long targetAscId, string abilityNodeGuid)
        {
#if UNITY_EDITOR
            lock (SyncRoot)
            {
                long nowMs = GetRealtimeMs();
                abilityActivationMs[GetAbilityKey(casterAscId, skillId)] = nowMs;
                WriteLine($"[Timing] AbilityActivated skillId={skillId} casterUnit={casterUnitId} casterAsc={casterAscId} targetAsc={targetAscId} sinceLoadMs={FormatDurationSinceLoad(nowMs)} loadContext={battleLoadContext} abilityNodeGuid={abilityNodeGuid} realtimeMs={nowMs}");
            }
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogDamageApplied(string skillId, long casterUnitId, long casterAscId, long targetAscId, string nodeGuid, float damage, float hpBefore, float hpAfter)
        {
#if UNITY_EDITOR
            lock (SyncRoot)
            {
                long nowMs = GetRealtimeMs();
                bool hasActivation = abilityActivationMs.TryGetValue(GetAbilityKey(casterAscId, skillId), out long activateMs);
                WriteLine($"[Timing] DamageApplied skillId={skillId} casterUnit={casterUnitId} casterAsc={casterAscId} targetAsc={targetAscId} sinceLoadMs={FormatDurationSinceLoad(nowMs)} afterActivateMs={FormatDurationSinceActivation(nowMs, activateMs, hasActivation)} nodeGuid={nodeGuid} damage={damage:F3} hpBefore={hpBefore:F3} hpAfter={hpAfter:F3} realtimeMs={nowMs}");
            }
#endif
        }

        [Conditional("UNITY_EDITOR")]
        public static void LogTimingSinceLoad(string category, string message)
        {
#if UNITY_EDITOR
            lock (SyncRoot)
            {
                long nowMs = GetRealtimeMs();
                WriteLine($"[Timing] {category} sinceLoadMs={FormatDurationSinceLoad(nowMs)} loadContext={battleLoadContext} realtimeMs={nowMs} {message}");
            }
#endif
        }

#if UNITY_EDITOR
        private static void WriteLine(string message)
        {
            EnsureLogFile();
            File.AppendAllText(currentLogFilePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }

        private static long GetRealtimeMs()
        {
            return (long)(UnityEngine.Time.realtimeSinceStartup * 1000f);
        }

        private static string GetAbilityKey(long casterAscId, string skillId)
        {
            return $"{casterAscId}:{skillId}";
        }

        private static string FormatDuration(long durationMs)
        {
            return durationMs >= 0 ? durationMs.ToString() : "NA";
        }

        private static string FormatDurationSinceLoad(long nowMs)
        {
            return hasBattleLoadComplete ? FormatDuration(nowMs - battleLoadCompleteMs) : "NA";
        }

        private static string FormatDurationSinceActivation(long nowMs, long activateMs, bool hasActivation)
        {
            return hasActivation ? FormatDuration(nowMs - activateMs) : "NA";
        }

        private static void EnsureLogFile()
        {
            bool isPlaying = UnityEditor.EditorApplication.isPlaying;
            if (string.IsNullOrEmpty(currentLogFilePath) || (isPlaying && !wasPlaying))
            {
                string logDirectory = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Temp", "SkillDiagLogs"));
                Directory.CreateDirectory(logDirectory);

                string runType = isPlaying ? "play" : "editor";
                currentLogFilePath = Path.Combine(logDirectory, $"skill_diag_{runType}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log");
            }

            wasPlaying = isPlaying;
        }
#endif
    }
}
