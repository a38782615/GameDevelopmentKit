using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public sealed class RecentPrefabWindow : EditorWindow
    {
        private const string MenuPath = "Game/Tool/Recent Prefabs";

        private string m_SearchText = string.Empty;
        private Vector2 m_ScrollPosition;

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            RecentPrefabWindow window = GetWindow<RecentPrefabWindow>("Recent Prefabs");
            window.minSize = new Vector2(720f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            RecentPrefabAccessService.HistoryChanged += Repaint;
        }

        private void OnDisable()
        {
            RecentPrefabAccessService.HistoryChanged -= Repaint;
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawTips();
            DrawPrefabList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string newSearchText = GUILayout.TextField(m_SearchText, EditorStyles.toolbarTextField);
            if (!string.Equals(newSearchText, m_SearchText, StringComparison.Ordinal))
            {
                m_SearchText = newSearchText;
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                RecentPrefabAccessService.ClearHistory();
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawTips()
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Tracks recently opened prefabs, prefabs entered in Prefab Mode, and prefabs selected in the Project window.", MessageType.Info);
        }

        private void DrawPrefabList()
        {
            IReadOnlyList<RecentPrefabAccessService.RecentPrefabInfo> entries = RecentPrefabAccessService.GetRecentPrefabs();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Recent Prefabs", EditorStyles.boldLabel);

            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            int visibleCount = 0;
            foreach (RecentPrefabAccessService.RecentPrefabInfo entry in entries)
            {
                if (!IsVisible(entry))
                {
                    continue;
                }

                visibleCount++;
                DrawEntry(entry);
                EditorGUILayout.Space(4f);
            }

            if (visibleCount == 0)
            {
                EditorGUILayout.HelpBox("No prefab entries match the current filter.", MessageType.None);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool IsVisible(RecentPrefabAccessService.RecentPrefabInfo entry)
        {
            if (string.IsNullOrWhiteSpace(m_SearchText))
            {
                return true;
            }

            return entry.DisplayName.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   entry.AssetPath.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawEntry(RecentPrefabAccessService.RecentPrefabInfo entry)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.AssetPath);
                    EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
                }

                if (GUILayout.Button("Open", GUILayout.Width(56f)))
                {
                    RecentPrefabAccessService.OpenPrefab(entry.Guid);
                }
            }
        }
    }
}
