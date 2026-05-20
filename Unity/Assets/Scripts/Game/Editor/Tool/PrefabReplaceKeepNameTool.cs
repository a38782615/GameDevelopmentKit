using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    public sealed class PrefabReplaceKeepNameTool : EditorWindow
    {
        private const string MenuPath = "Game/Tool/Prefab Replace Keep Name";
        private const string PrefabFilter = "t:Prefab";

        private static readonly List<PrefabOption> s_PrefabOptions = new List<PrefabOption>();
        private static readonly string[] s_EmptyDisplayOptions = { "<No Prefab Found>" };

        private Vector2 m_ScrollPosition;
        private string m_SearchText = string.Empty;
        private int m_SelectedIndex = -1;

        [MenuItem(MenuPath)]
        private static void OpenWindow()
        {
            PrefabReplaceKeepNameTool window = GetWindow<PrefabReplaceKeepNameTool>("Prefab Replace");
            window.minSize = new Vector2(520f, 360f);
            window.RefreshPrefabOptions(true);
        }

        private void OnEnable()
        {
            RefreshPrefabOptions(false);
        }

        private void OnFocus()
        {
            RefreshPrefabOptions(false);
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSelectionInfo();
            DrawPrefabSelection();
            DrawReplaceButton();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            string searchText = GUILayout.TextField(m_SearchText, GUI.skin.FindStyle("ToolbarSeachTextField"));
            if (!string.Equals(searchText, m_SearchText, StringComparison.Ordinal))
            {
                m_SearchText = searchText;
                ClampSelectedIndex();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
            {
                RefreshPrefabOptions(true);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionInfo()
        {
            Transform[] selectedTransforms = Selection.transforms;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Will replace {selectedTransforms.Length} selected object(s) and keep each original object name.",
                MessageType.Info);

            using (new EditorGUI.DisabledScope(true))
            {
                foreach (Transform selectedTransform in selectedTransforms)
                {
                    EditorGUILayout.ObjectField(selectedTransform, typeof(Transform), true);
                }
            }
        }

        private void DrawPrefabSelection()
        {
            List<PrefabOption> filteredOptions = GetFilteredOptions();
            string[] displayOptions = filteredOptions.Count > 0
                ? filteredOptions.Select(option => option.DisplayName).ToArray()
                : s_EmptyDisplayOptions;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Target Prefab", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(filteredOptions.Count == 0))
            {
                int currentIndex = Mathf.Clamp(m_SelectedIndex, 0, Mathf.Max(0, filteredOptions.Count - 1));
                int selectedIndex = EditorGUILayout.Popup("Prefab", currentIndex, displayOptions);
                if (selectedIndex != currentIndex)
                {
                    m_SelectedIndex = selectedIndex;
                }
                else if (m_SelectedIndex != currentIndex)
                {
                    m_SelectedIndex = currentIndex;
                }
            }

            EditorGUILayout.Space(4f);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition, GUILayout.Height(180f));
            foreach (PrefabOption option in filteredOptions)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    bool isSelected = filteredOptions.Count > 0 && filteredOptions[Mathf.Clamp(m_SelectedIndex, 0, filteredOptions.Count - 1)] == option;
                    if (GUILayout.Toggle(isSelected, GUIContent.none, GUILayout.Width(18f)))
                    {
                        m_SelectedIndex = filteredOptions.IndexOf(option);
                    }

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField(option.Prefab, typeof(GameObject), false);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            PrefabOption selectedOption = GetSelectedOption(filteredOptions);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Selected Asset", selectedOption?.Prefab, typeof(GameObject), false);
                EditorGUILayout.TextField("Asset Path", selectedOption?.AssetPath ?? string.Empty);
            }
        }

        private void DrawReplaceButton()
        {
            EditorGUILayout.Space();
            bool canReplace = Selection.transforms.Length > 0 && GetSelectedOption(GetFilteredOptions()) != null;
            using (new EditorGUI.DisabledScope(!canReplace))
            {
                if (GUILayout.Button("Replace Selection", GUILayout.Height(32f)))
                {
                    ReplaceSelection();
                }
            }
        }

        private void RefreshPrefabOptions(bool forceRepaint)
        {
            s_PrefabOptions.Clear();
            string[] guids = AssetDatabase.FindAssets(PrefabFilter);
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                {
                    continue;
                }

                s_PrefabOptions.Add(new PrefabOption(prefab, assetPath));
            }

            s_PrefabOptions.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            ClampSelectedIndex();

            if (forceRepaint)
            {
                Repaint();
            }
        }

        private List<PrefabOption> GetFilteredOptions()
        {
            if (string.IsNullOrWhiteSpace(m_SearchText))
            {
                return s_PrefabOptions;
            }

            return s_PrefabOptions
                .Where(option => option.DisplayName.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 option.AssetPath.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }

        private PrefabOption GetSelectedOption(List<PrefabOption> filteredOptions)
        {
            if (filteredOptions.Count == 0)
            {
                return null;
            }

            ClampSelectedIndex();
            if (m_SelectedIndex < 0 || m_SelectedIndex >= filteredOptions.Count)
            {
                return filteredOptions[0];
            }

            return filteredOptions[m_SelectedIndex];
        }

        private void ClampSelectedIndex()
        {
            int count = GetFilteredOptionsInternalCount();
            if (count <= 0)
            {
                m_SelectedIndex = -1;
                return;
            }

            if (m_SelectedIndex < 0 || m_SelectedIndex >= count)
            {
                m_SelectedIndex = 0;
            }
        }

        private int GetFilteredOptionsInternalCount()
        {
            if (string.IsNullOrWhiteSpace(m_SearchText))
            {
                return s_PrefabOptions.Count;
            }

            return s_PrefabOptions.Count(option =>
                option.DisplayName.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                option.AssetPath.IndexOf(m_SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void ReplaceSelection()
        {
            List<PrefabOption> filteredOptions = GetFilteredOptions();
            PrefabOption selectedOption = GetSelectedOption(filteredOptions);
            if (selectedOption == null)
            {
                EditorUtility.DisplayDialog("Prefab Replace", "Please select a prefab first.", "OK");
                return;
            }

            Transform[] selectedTransforms = Selection.transforms;
            if (selectedTransforms.Length == 0)
            {
                EditorUtility.DisplayDialog("Prefab Replace", "Please select object(s) in Hierarchy first.", "OK");
                return;
            }

            List<GameObject> replacedObjects = new List<GameObject>(selectedTransforms.Length);
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Replace Selection Keep Name");

            try
            {
                foreach (Transform selectedTransform in selectedTransforms)
                {
                    if (selectedTransform == null)
                    {
                        continue;
                    }

                    GameObject newObject = ReplaceSingle(selectedTransform.gameObject, selectedOption.Prefab);
                    if (newObject != null)
                    {
                        replacedObjects.Add(newObject);
                    }
                }
            }
            finally
            {
                Undo.CollapseUndoOperations(undoGroup);
            }

            if (replacedObjects.Count > 0)
            {
                Selection.objects = replacedObjects.ToArray();
            }
        }

        private static GameObject ReplaceSingle(GameObject originalObject, GameObject prefab)
        {
            Scene scene = originalObject.scene;
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            if (newObject == null)
            {
                return null;
            }

            Undo.RegisterCreatedObjectUndo(newObject, "Create Replacement");

            Transform originalTransform = originalObject.transform;
            Transform newTransform = newObject.transform;
            Transform parent = originalTransform.parent;
            int siblingIndex = originalTransform.GetSiblingIndex();

            newTransform.SetParent(parent, false);
            newTransform.SetSiblingIndex(siblingIndex);

            CopyTransform(originalTransform, newTransform);

            newObject.name = originalObject.name;
            newObject.SetActive(originalObject.activeSelf);
            newObject.tag = originalObject.tag;
            newObject.layer = originalObject.layer;

            Undo.DestroyObjectImmediate(originalObject);
            EditorSceneManager.MarkSceneDirty(scene);
            return newObject;
        }

        private static void CopyTransform(Transform source, Transform destination)
        {
            if (source is RectTransform sourceRect && destination is RectTransform destinationRect)
            {
                destinationRect.anchorMin = sourceRect.anchorMin;
                destinationRect.anchorMax = sourceRect.anchorMax;
                destinationRect.anchoredPosition = sourceRect.anchoredPosition;
                destinationRect.anchoredPosition3D = sourceRect.anchoredPosition3D;
                destinationRect.sizeDelta = sourceRect.sizeDelta;
                destinationRect.pivot = sourceRect.pivot;
                destinationRect.localRotation = sourceRect.localRotation;
                destinationRect.localScale = sourceRect.localScale;
                destinationRect.offsetMin = sourceRect.offsetMin;
                destinationRect.offsetMax = sourceRect.offsetMax;
                return;
            }

            destination.localPosition = source.localPosition;
            destination.localRotation = source.localRotation;
            destination.localScale = source.localScale;
        }

        private sealed class PrefabOption
        {
            public PrefabOption(GameObject prefab, string assetPath)
            {
                Prefab = prefab;
                AssetPath = assetPath;
                DisplayName = $"{prefab.name} ({assetPath})";
            }

            public GameObject Prefab { get; }

            public string AssetPath { get; }

            public string DisplayName { get; }
        }
    }
}
