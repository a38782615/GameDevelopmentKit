using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class RecentPrefabAccessService
    {
        public sealed class RecentPrefabInfo
        {
            public string Guid { get; set; }

            public string AssetPath { get; set; }

            public string DisplayName { get; set; }

            public string LastAction { get; set; }

            public DateTime LastAccessTime { get; set; }
        }

        [Serializable]
        private sealed class RecentPrefabHistoryData
        {
            public List<RecentPrefabRecord> Records = new List<RecentPrefabRecord>();
        }

        [Serializable]
        private sealed class RecentPrefabRecord
        {
            public string Guid;
            public string LastAction;
            public long LastAccessTicks;
        }

        private const string EditorPrefsKey = "Game.Editor.RecentPrefabAccessService.History";
        private const int MaxHistoryCount = 30;

        private static readonly List<RecentPrefabRecord> s_Records = new List<RecentPrefabRecord>();
        private static bool s_IgnoreSelectionChanged;

        public static event Action HistoryChanged;

        static RecentPrefabAccessService()
        {
            LoadHistory();
            CleanupInvalidRecords(false);
            Selection.selectionChanged += OnSelectionChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            PrefabStage.prefabStageOpened += OnPrefabStageOpened;
        }

        public static IReadOnlyList<RecentPrefabInfo> GetRecentPrefabs()
        {
            CleanupInvalidRecords(false);
            return s_Records
                .Select(CreateInfo)
                .Where(info => info != null)
                .ToList();
        }

        public static void ClearHistory()
        {
            if (s_Records.Count == 0)
            {
                return;
            }

            s_Records.Clear();
            SaveHistory();
            NotifyHistoryChanged();
        }

        public static void ShowPrefab(string guid)
        {
            GameObject prefab = LoadPrefab(guid);
            if (prefab == null)
            {
                return;
            }

            SelectPrefab(prefab, false);
            AddOrUpdate(guid, "Show");
        }

        public static void OpenPrefab(string guid)
        {
            GameObject prefab = LoadPrefab(guid);
            if (prefab == null)
            {
                return;
            }

            AssetDatabase.OpenAsset(prefab);
            AddOrUpdate(guid, "Open");
        }

        public static void LocatePrefab(string guid)
        {
            GameObject prefab = LoadPrefab(guid);
            if (prefab == null)
            {
                return;
            }

            EditorUtility.FocusProjectWindow();
            SelectPrefab(prefab, true);
            AddOrUpdate(guid, "Locate");
        }

        [OnOpenAsset(-200)]
        private static bool OnOpenAsset(int instanceId, int line)
        {
            string assetPath = AssetDatabase.GetAssetPath(instanceId);
            if (!IsPrefabPath(assetPath))
            {
                return false;
            }

            AddOrUpdateByPath(assetPath, "Open");
            return false;
        }

        private static void OnSelectionChanged()
        {
            if (s_IgnoreSelectionChanged)
            {
                return;
            }

            UnityEngine.Object activeObject = Selection.activeObject;
            if (activeObject == null)
            {
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(activeObject);
            if (!IsPrefabPath(assetPath))
            {
                return;
            }

            AddOrUpdateByPath(assetPath, "Select");
        }

        private static void OnProjectChanged()
        {
            CleanupInvalidRecords(true);
        }

        private static void OnPrefabStageOpened(PrefabStage prefabStage)
        {
            if (prefabStage == null || !IsPrefabPath(prefabStage.assetPath))
            {
                return;
            }

            AddOrUpdateByPath(prefabStage.assetPath, "Open");
        }

        private static void SelectPrefab(GameObject prefab, bool pingObject)
        {
            s_IgnoreSelectionChanged = true;
            Selection.activeObject = prefab;
            if (pingObject)
            {
                EditorGUIUtility.PingObject(prefab);
            }

            EditorApplication.delayCall -= ResetIgnoreSelectionChanged;
            EditorApplication.delayCall += ResetIgnoreSelectionChanged;
        }

        private static void ResetIgnoreSelectionChanged()
        {
            s_IgnoreSelectionChanged = false;
            EditorApplication.delayCall -= ResetIgnoreSelectionChanged;
        }

        private static void AddOrUpdateByPath(string assetPath, string action)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            AddOrUpdate(guid, action);
        }

        private static void AddOrUpdate(string guid, string action)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            RecentPrefabRecord record = s_Records.FirstOrDefault(item => item.Guid == guid);
            if (record != null)
            {
                s_Records.Remove(record);
            }
            else
            {
                record = new RecentPrefabRecord
                {
                    Guid = guid,
                };
            }

            record.LastAction = action;
            record.LastAccessTicks = DateTime.Now.Ticks;
            s_Records.Insert(0, record);

            if (s_Records.Count > MaxHistoryCount)
            {
                s_Records.RemoveRange(MaxHistoryCount, s_Records.Count - MaxHistoryCount);
            }

            SaveHistory();
            NotifyHistoryChanged();
        }

        private static RecentPrefabInfo CreateInfo(RecentPrefabRecord record)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(record.Guid);
            if (!IsPrefabPath(assetPath))
            {
                return null;
            }

            return new RecentPrefabInfo
            {
                Guid = record.Guid,
                AssetPath = assetPath,
                DisplayName = System.IO.Path.GetFileNameWithoutExtension(assetPath),
                LastAction = string.IsNullOrEmpty(record.LastAction) ? "Access" : record.LastAction,
                LastAccessTime = new DateTime(record.LastAccessTicks),
            };
        }

        private static GameObject LoadPrefab(string guid)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!IsPrefabPath(assetPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        }

        private static void LoadHistory()
        {
            s_Records.Clear();
            string json = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            RecentPrefabHistoryData historyData = JsonUtility.FromJson<RecentPrefabHistoryData>(json);
            if (historyData == null || historyData.Records == null)
            {
                return;
            }

            s_Records.AddRange(historyData.Records
                .Where(record => record != null && !string.IsNullOrEmpty(record.Guid))
                .OrderByDescending(record => record.LastAccessTicks));
        }

        private static void SaveHistory()
        {
            RecentPrefabHistoryData historyData = new RecentPrefabHistoryData
            {
                Records = s_Records,
            };
            string json = JsonUtility.ToJson(historyData);
            EditorPrefs.SetString(EditorPrefsKey, json);
        }

        private static void CleanupInvalidRecords(bool notifyWhenChanged)
        {
            int removedCount = s_Records.RemoveAll(record =>
            {
                if (record == null || string.IsNullOrEmpty(record.Guid))
                {
                    return true;
                }

                string assetPath = AssetDatabase.GUIDToAssetPath(record.Guid);
                return !IsPrefabPath(assetPath);
            });

            if (removedCount <= 0)
            {
                return;
            }

            SaveHistory();
            if (notifyWhenChanged)
            {
                NotifyHistoryChanged();
            }
        }

        private static bool IsPrefabPath(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
        }

        private static void NotifyHistoryChanged()
        {
            HistoryChanged?.Invoke();
        }
    }
}
