using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace ET.Editor
{
    public static class UIFightPrefabBuilder
    {
        private const string FinalizeMenuPath = "ET/GenAtom/Finalize UIFight Prefab";
        private const string PrefabPath = "Assets/Res/UI/UIForm/GenAtom/UIFormFight.prefab";
        private const string BtmBarPrefabPath = "Assets/Res/UI/UIEntity/BtmBar.prefab";
        private const string BtmBarNodeName = "BtmBar_BtmBar";
        private const string MonoUIFormFightTypeName = "ET.Client.MonoUIFormFight, Game.ET.Code.ModelView";
        private const string MonoUIWidgetBtmBarTypeName = "ET.Client.MonoUIWidgetBtmBar, Game.ET.Code.ModelView";
        private const string PendingRefreshKey = "UIFightPrefabBuilder.PendingRefresh";
        private const string AutoRunMarkerPath = "Temp/UIFightPrefabBuilder.run";

        [MenuItem(FinalizeMenuPath)]
        public static void FinalizePrefab()
        {
            EnsurePrefabHasBtmBarAndBind();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIFightPrefabBuilder] Finalized prefab: {PrefabPath}");
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            TryScheduleAutoRun();

            if (!SessionState.GetBool(PendingRefreshKey, false))
            {
                return;
            }

            EditorApplication.delayCall -= TryRefreshPendingBindData;
            EditorApplication.delayCall += TryRefreshPendingBindData;
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            TryScheduleAutoRun();
        }

        private static void EnsurePrefabHasBtmBarAndBind()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EnsureBtmBar(prefabRoot);
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

        private static void TryScheduleAutoRun()
        {
            if (!File.Exists(AutoRunMarkerPath))
            {
                return;
            }

            EditorApplication.delayCall -= RunAutoFinalize;
            EditorApplication.delayCall += RunAutoFinalize;
        }

        private static void RunAutoFinalize()
        {
            EditorApplication.delayCall -= RunAutoFinalize;

            if (!File.Exists(AutoRunMarkerPath))
            {
                return;
            }

            try
            {
                FinalizePrefab();
            }
            finally
            {
                if (File.Exists(AutoRunMarkerPath))
                {
                    File.Delete(AutoRunMarkerPath);
                }
            }
        }

        private static void EnsureBtmBar(GameObject prefabRoot)
        {
            Transform existing = prefabRoot.transform.Find(BtmBarNodeName);
            if (existing != null)
            {
                return;
            }

            GameObject btmBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BtmBarPrefabPath);
            if (btmBarPrefab == null)
            {
                throw new FileNotFoundException($"Bottom bar prefab not found: {BtmBarPrefabPath}");
            }

            GameObject btmBarInstance = PrefabUtility.InstantiatePrefab(btmBarPrefab, prefabRoot.transform) as GameObject;
            if (btmBarInstance == null)
            {
                throw new InvalidOperationException($"Instantiate prefab failed: {BtmBarPrefabPath}");
            }

            btmBarInstance.name = BtmBarNodeName;
            RectTransform rectTransform = btmBarInstance.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.SetParent(prefabRoot.transform, false);
                rectTransform.localScale = Vector3.one;
                rectTransform.localPosition = Vector3.zero;
                rectTransform.localRotation = Quaternion.identity;
                rectTransform.anchoredPosition = Vector2.zero;
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
                Component formComponent = prefabRoot.GetComponent(ResolveType(MonoUIFormFightTypeName));
                if (formComponent == null)
                {
                    throw new InvalidOperationException("MonoUIFormFight component not found.");
                }

                Component btmBarComponent = FindRequiredChild(prefabRoot.transform, BtmBarNodeName)
                    .GetComponent(ResolveType(MonoUIWidgetBtmBarTypeName));
                if (btmBarComponent == null)
                {
                    throw new InvalidOperationException("MonoUIWidgetBtmBar component not found.");
                }

                TrySetObjectReference(formComponent, "m_BtmBarBtmBar", btmBarComponent);

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
            MonoBehaviour monoBehaviour = root.GetComponent(ResolveType(MonoUIFormFightTypeName)) as MonoBehaviour;
            if (monoBehaviour == null)
            {
                throw new InvalidOperationException("MonoUIFormFight MonoBehaviour not found.");
            }

            MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (monoScript == null)
            {
                throw new InvalidOperationException("MonoUIFormFight MonoScript not found.");
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

        private static Transform FindRequiredChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            if (child == null)
            {
                throw new InvalidOperationException($"Child not found: {path}");
            }

            return child;
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
