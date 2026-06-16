using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ET.Editor
{
    public static class UIMainPrefabBuilder
    {
        private const string FinalizeMenuPath = "ET/GenAtom/Finalize UIMain Prefab";
        private const string PrefabPath = "Assets/Res/UI/UIForm/GenAtom/UIMain.prefab";
        private const string TopBarPrefabPath = "Assets/Res/UI/UIEntity/TopBar.prefab";
        private const string BtmBarNodeName = "BtmBar_BtmBar";
        private const string TopBarNodeName = "TopBar_TopBar";
        private const string MonoUIFormMainTypeName = "ET.Client.MonoUIFormMain, Game.ET.Code.ModelView";
        private const string MonoUIWidgetBtmBarTypeName = "ET.Client.MonoUIWidgetBtmBar, Game.ET.Code.ModelView";
        private const string MonoUIWidgetTopBarTypeName = "ET.Client.MonoUIWidgetTopBar, Game.ET.Code.ModelView";
        private const string PendingRefreshKey = "UIMainPrefabBuilder.PendingRefresh";

        [MenuItem(FinalizeMenuPath)]
        public static void FinalizePrefab()
        {
            EnsurePrefabHasTopBarAndBind();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIMainPrefabBuilder] Finalized prefab: {PrefabPath}");
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!SessionState.GetBool(PendingRefreshKey, false))
            {
                return;
            }

            EditorApplication.delayCall -= TryRefreshPendingBindData;
            EditorApplication.delayCall += TryRefreshPendingBindData;
        }

        private static void EnsurePrefabHasTopBarAndBind()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EnsureTopBar(prefabRoot);
                TryGenerateMonoCodeBind(prefabRoot);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException($"Save prefab failed: {PrefabPath}");
                }

                ScheduleBindRefresh();
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void EnsureTopBar(GameObject prefabRoot)
        {
            Transform existing = prefabRoot.transform.Find(TopBarNodeName);
            if (existing != null)
            {
                return;
            }

            Type topBarType = ResolveType(MonoUIWidgetTopBarTypeName);
            Component existingComponent = prefabRoot.GetComponentInChildren(topBarType, true);
            if (existingComponent != null)
            {
                RectTransform existingRectTransform = existingComponent.GetComponent<RectTransform>();
                existingComponent.name = TopBarNodeName;
                if (existingRectTransform != null && existingRectTransform.parent != prefabRoot.transform)
                {
                    existingRectTransform.SetParent(prefabRoot.transform, false);
                }

                return;
            }

            GameObject topBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TopBarPrefabPath);
            if (topBarPrefab == null)
            {
                throw new InvalidOperationException($"Top bar prefab not found: {TopBarPrefabPath}");
            }

            GameObject topBarInstance = PrefabUtility.InstantiatePrefab(topBarPrefab, prefabRoot.transform) as GameObject;
            if (topBarInstance == null)
            {
                throw new InvalidOperationException($"Instantiate prefab failed: {TopBarPrefabPath}");
            }

            topBarInstance.name = TopBarNodeName;
            RectTransform rectTransform = topBarInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.SetParent(prefabRoot.transform, false);
                rectTransform.SetAsLastSibling();
            }
        }

        private static void ScheduleBindRefresh()
        {
            SessionState.SetBool(PendingRefreshKey, true);
            EditorApplication.delayCall -= TryRefreshPendingBindData;
            EditorApplication.delayCall += TryRefreshPendingBindData;
        }

        private static void TryRefreshPendingBindData()
        {
            if (!SessionState.GetBool(PendingRefreshKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= TryRefreshPendingBindData;
                EditorApplication.delayCall += TryRefreshPendingBindData;
                return;
            }

            SessionState.EraseBool(PendingRefreshKey);

            try
            {
                RefreshMonoCodeBindSerialization();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void RefreshMonoCodeBindSerialization()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Component formComponent = prefabRoot.GetComponent(ResolveType(MonoUIFormMainTypeName));
                if (formComponent == null)
                {
                    throw new InvalidOperationException("MonoUIFormMain component not found.");
                }

                Component btmBarComponent = FindOptionalChildComponent(prefabRoot.transform, BtmBarNodeName, MonoUIWidgetBtmBarTypeName);
                Component topBarComponent = FindOptionalChildComponent(prefabRoot.transform, TopBarNodeName, MonoUIWidgetTopBarTypeName);

                TrySetObjectReference(formComponent, "m_BtmBarBtmBar", btmBarComponent);
                TrySetObjectReference(formComponent, "m_TopBarTopBar", topBarComponent);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException($"Refresh bind serialization failed: {PrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void TryGenerateMonoCodeBind(GameObject root)
        {
            MonoBehaviour monoBehaviour = root.GetComponent(ResolveType(MonoUIFormMainTypeName)) as MonoBehaviour;
            if (monoBehaviour == null)
            {
                throw new InvalidOperationException("MonoUIFormMain MonoBehaviour not found.");
            }

            MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (monoScript == null)
            {
                throw new InvalidOperationException("MonoUIFormMain MonoScript not found.");
            }

            Type binderType = Type.GetType("CodeBind.Editor.MonoCodeBinder, CodeBind.Editor");
            if (binderType == null)
            {
                throw new InvalidOperationException("CodeBind.Editor.MonoCodeBinder type not found.");
            }

            object binder = Activator.CreateInstance(binderType, monoScript, root.transform, '_');
            MethodInfo tryGenerateBindCodeMethod = binderType.GetMethod("TryGenerateBindCode", BindingFlags.Instance | BindingFlags.Public);
            if (tryGenerateBindCodeMethod == null)
            {
                throw new InvalidOperationException("MonoCodeBinder.TryGenerateBindCode not found.");
            }

            tryGenerateBindCodeMethod.Invoke(binder, null);
        }

        private static Component FindOptionalChildComponent(Transform parent, string path, string typeName)
        {
            Transform child = parent.Find(path);
            if (child == null)
            {
                return null;
            }

            return child.GetComponent(ResolveType(typeName));
        }

        private static void TrySetObjectReference(Component component, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                return;
            }

            property.objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Type ResolveType(string assemblyQualifiedTypeName)
        {
            Type type = Type.GetType(assemblyQualifiedTypeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type not found: {assemblyQualifiedTypeName}");
            }

            return type;
        }
    }
}
