using System;
using System.IO;
using System.Reflection;
using Game;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Editor
{
    public static class UIUnitAttributePrefabBuilder
    {
        private const string MenuPath = "ET/GenAtom/Rebuild UIUnitAttribute Prefab";
        private const string FinalizeMenuPath = "ET/GenAtom/Finalize UIUnitAttribute Prefab";
        private const string PrefabPath = "Assets/Res/UI/UIForm/GenAtom/UIUnitAttribute.prefab";
        private const string MonoUIFormTypeName = "ET.Client.MonoUIFormUnitAttribute, Game.ET.Code.ModelView";
        private const string MonoRowTypeName = "ET.Client.MonoUIUnitAttributeRow, Game.ET.Code.ModelView";
        private const string PendingNormalizeKey = "UIUnitAttributePrefabBuilder.PendingNormalize";
        private static readonly Color playerAccentColor = new Color(0.29f, 0.78f, 0.47f, 1f);
        private static readonly Color monsterAccentColor = new Color(0.95f, 0.43f, 0.35f, 1f);

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            string directory = Path.GetDirectoryName(PrefabPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            GameObject root = null;
            try
            {
                root = CreateRoot();
                TryGenerateMonoCodeBind(root);
                RefreshMonoCodeBindSerialization(root);
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Save prefab failed: {PrefabPath}");
                }

                ScheduleSavedPrefabNormalization();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[UIUnitAttributePrefabBuilder] Rebuilt prefab: {PrefabPath}");
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [MenuItem(FinalizeMenuPath)]
        public static void FinalizePrefab()
        {
            NormalizeSavedPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIUnitAttributePrefabBuilder] Finalized prefab: {PrefabPath}");
        }

        private static GameObject CreateRoot()
        {
            GameObject root = new GameObject(
                "UIUnitAttribute",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasRenderer),
                typeof(GraphicRaycaster),
                typeof(RaycastGraphic));

            root.AddComponent(ResolveType(MonoUIFormTypeName));

            RectTransform rootRect = root.GetComponent<RectTransform>();
            StretchToParent(rootRect);

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            RaycastGraphic raycastGraphic = root.GetComponent<RaycastGraphic>();
            raycastGraphic.color = new Color(0f, 0f, 0f, 0f);
            raycastGraphic.raycastTarget = false;

            CreatePanel(root.transform, true);
            CreatePanel(root.transform, false);
            return root;
        }

        private static RectTransform CreatePanel(Transform parent, bool isPlayerPanel)
        {
            string prefix = isPlayerPanel ? "Player" : "Monster";
            Color accentColor = isPlayerPanel ? playerAccentColor : monsterAccentColor;

            GameObject panel = new GameObject(
                $"{prefix}Panel_RectTransform",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            panel.transform.SetParent(parent, false);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(isPlayerPanel ? 0f : 1f, 1f);
            panelRect.anchorMax = new Vector2(isPlayerPanel ? 0f : 1f, 1f);
            panelRect.pivot = new Vector2(isPlayerPanel ? 0f : 1f, 1f);
            panelRect.anchoredPosition = new Vector2(isPlayerPanel ? 20f : -20f, -20f);
            panelRect.sizeDelta = new Vector2(320f, 0f);
            panelRect.localScale = Vector3.one;

            Image panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
            panelImage.raycastTarget = false;

            VerticalLayoutGroup layoutGroup = panel.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(16, 16, 16, 16);
            layoutGroup.spacing = 6f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = panel.GetComponent<ContentSizeFitter>();
            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Text titleText = CreateText($"{prefix}Title_Text", panel.transform, isPlayerPanel ? "Player" : "Monster", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
            titleText.color = accentColor;
            SetLayoutElement(titleText.gameObject, 34f, 0f, 0f);

            Text tagsText = CreateText($"{prefix}Tags_Text", panel.transform, "Tags: None", 16, FontStyle.Normal, TextAnchor.UpperLeft);
            tagsText.color = new Color(0.83f, 0.88f, 0.95f, 0.92f);
            tagsText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tagsText.verticalOverflow = VerticalWrapMode.Overflow;
            SetLayoutElement(tagsText.gameObject, 40f, 0f, 0f);

            GameObject rowsRoot = new GameObject(
                $"{prefix}Rows_RectTransform",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter));
            rowsRoot.transform.SetParent(panel.transform, false);

            VerticalLayoutGroup rowsLayoutGroup = rowsRoot.GetComponent<VerticalLayoutGroup>();
            rowsLayoutGroup.padding = new RectOffset(0, 0, 4, 0);
            rowsLayoutGroup.spacing = 4f;
            rowsLayoutGroup.childAlignment = TextAnchor.UpperLeft;
            rowsLayoutGroup.childControlWidth = true;
            rowsLayoutGroup.childControlHeight = false;
            rowsLayoutGroup.childForceExpandWidth = true;
            rowsLayoutGroup.childForceExpandHeight = false;

            ContentSizeFitter rowsContentSizeFitter = rowsRoot.GetComponent<ContentSizeFitter>();
            rowsContentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            rowsContentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            SetLayoutElement(rowsRoot, 0f, 0f, 0f);

            Text categoryTemplate = CreateText($"{prefix}CategoryTemplate_Text", rowsRoot.transform, "Category", 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            categoryTemplate.color = accentColor;
            SetLayoutElement(categoryTemplate.gameObject, 28f, 0f, 0f);
            categoryTemplate.gameObject.SetActive(false);

            CreateRowTemplate(rowsRoot.transform, prefix);
            return panelRect;
        }

        private static void CreateRowTemplate(Transform parent, string prefix)
        {
            GameObject row = new GameObject(
                $"{prefix}ItemTemplate_AttributeRowTemplate",
                typeof(RectTransform),
                typeof(HorizontalLayoutGroup),
                typeof(LayoutElement),
                ResolveType(MonoRowTypeName));
            row.transform.SetParent(parent, false);

            HorizontalLayoutGroup layoutGroup = row.GetComponent<HorizontalLayoutGroup>();
            layoutGroup.spacing = 12f;
            layoutGroup.padding = new RectOffset(0, 0, 0, 0);
            layoutGroup.childAlignment = TextAnchor.MiddleLeft;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            LayoutElement layoutElement = row.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 24f;
            layoutElement.flexibleWidth = 1f;

            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.localScale = Vector3.one;

            Text labelText = CreateText("Label_Text", row.transform, "Label", 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            labelText.color = new Color(0.95f, 0.97f, 1f, 0.94f);
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 150f;
            labelLayout.flexibleWidth = 1f;

            Text valueText = CreateText("Value_Text", row.transform, "0", 16, FontStyle.Bold, TextAnchor.MiddleRight);
            valueText.color = new Color(1f, 0.89f, 0.48f, 0.98f);
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 110f;
            valueLayout.flexibleWidth = 0f;

            row.SetActive(false);
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text textComponent = textObject.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = anchor;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Overflow;
            textComponent.raycastTarget = false;

            RectTransform rectTransform = textComponent.rectTransform;
            rectTransform.localScale = Vector3.one;
            return textComponent;
        }

        private static void SetLayoutElement(GameObject gameObject, float preferredHeight, float preferredWidth, float flexibleWidth)
        {
            LayoutElement layoutElement = gameObject.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = preferredHeight;
            layoutElement.preferredWidth = preferredWidth;
            layoutElement.flexibleWidth = flexibleWidth;
        }

        private static void StretchToParent(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        private static void NormalizeSavedPrefab()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                RectTransform rootRect = prefabRoot.GetComponent<RectTransform>();
                if (rootRect != null)
                {
                    NormalizeRootRect(rootRect);
                }

                RefreshMonoCodeBindSerialization(prefabRoot);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException($"Normalize prefab failed: {PrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static void ScheduleSavedPrefabNormalization()
        {
            SessionState.SetBool(PendingNormalizeKey, true);
            EditorApplication.delayCall -= TryNormalizePendingSavedPrefab;
            EditorApplication.delayCall += TryNormalizePendingSavedPrefab;
        }

        [DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (!SessionState.GetBool(PendingNormalizeKey, false))
            {
                return;
            }

            EditorApplication.delayCall -= TryNormalizePendingSavedPrefab;
            EditorApplication.delayCall += TryNormalizePendingSavedPrefab;
        }

        private static void TryNormalizePendingSavedPrefab()
        {
            if (!SessionState.GetBool(PendingNormalizeKey, false))
            {
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall -= TryNormalizePendingSavedPrefab;
                EditorApplication.delayCall += TryNormalizePendingSavedPrefab;
                return;
            }

            SessionState.EraseBool(PendingNormalizeKey);

            try
            {
                NormalizeSavedPrefab();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        private static void NormalizeRootRect(RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static void RefreshMonoCodeBindSerialization(GameObject root)
        {
            Component formComponent = root.GetComponent(ResolveType(MonoUIFormTypeName));
            if (formComponent == null)
            {
                throw new InvalidOperationException("MonoUIFormUnitAttribute component not found.");
            }

            RectTransform playerPanelRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(root.transform, "PlayerPanel_RectTransform"));
            Text playerTitleText = GetRequiredComponent<Text>(FindRequiredChild(playerPanelRectTransform, "PlayerTitle_Text"));
            Text playerTagsText = GetRequiredComponent<Text>(FindRequiredChild(playerPanelRectTransform, "PlayerTags_Text"));
            RectTransform playerRowsRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(playerPanelRectTransform, "PlayerRows_RectTransform"));
            Text playerCategoryTemplateText = GetRequiredComponent<Text>(FindRequiredChild(playerRowsRectTransform, "PlayerCategoryTemplate_Text"));
            Component playerItemTemplate = GetRequiredComponent<Component>(FindRequiredChild(playerRowsRectTransform, "PlayerItemTemplate_AttributeRowTemplate"), ResolveType(MonoRowTypeName));

            RectTransform monsterPanelRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(root.transform, "MonsterPanel_RectTransform"));
            Text monsterTitleText = GetRequiredComponent<Text>(FindRequiredChild(monsterPanelRectTransform, "MonsterTitle_Text"));
            Text monsterTagsText = GetRequiredComponent<Text>(FindRequiredChild(monsterPanelRectTransform, "MonsterTags_Text"));
            RectTransform monsterRowsRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(monsterPanelRectTransform, "MonsterRows_RectTransform"));
            Text monsterCategoryTemplateText = GetRequiredComponent<Text>(FindRequiredChild(monsterRowsRectTransform, "MonsterCategoryTemplate_Text"));
            Component monsterItemTemplate = GetRequiredComponent<Component>(FindRequiredChild(monsterRowsRectTransform, "MonsterItemTemplate_AttributeRowTemplate"), ResolveType(MonoRowTypeName));

            TrySetObjectReference(formComponent, "m_PlayerPanelRectTransform", playerPanelRectTransform);
            TrySetObjectReference(formComponent, "m_PlayerTitleText", playerTitleText);
            TrySetObjectReference(formComponent, "m_PlayerTagsText", playerTagsText);
            TrySetObjectReference(formComponent, "m_PlayerRowsRectTransform", playerRowsRectTransform);
            TrySetObjectReference(formComponent, "m_PlayerCategoryTemplateText", playerCategoryTemplateText);
            TrySetObjectReference(formComponent, "m_PlayerItemTemplateAttributeRowTemplate", playerItemTemplate);

            TrySetObjectReference(formComponent, "m_MonsterPanelRectTransform", monsterPanelRectTransform);
            TrySetObjectReference(formComponent, "m_MonsterTitleText", monsterTitleText);
            TrySetObjectReference(formComponent, "m_MonsterTagsText", monsterTagsText);
            TrySetObjectReference(formComponent, "m_MonsterRowsRectTransform", monsterRowsRectTransform);
            TrySetObjectReference(formComponent, "m_MonsterCategoryTemplateText", monsterCategoryTemplateText);
            TrySetObjectReference(formComponent, "m_MonsterItemTemplateAttributeRowTemplate", monsterItemTemplate);

            RefreshRowBind(playerItemTemplate);
            RefreshRowBind(monsterItemTemplate);
        }

        private static void RefreshRowBind(Component rowComponent)
        {
            Text labelText = GetRequiredComponent<Text>(FindRequiredChild(rowComponent.transform, "Label_Text"));
            Text valueText = GetRequiredComponent<Text>(FindRequiredChild(rowComponent.transform, "Value_Text"));

            TrySetObjectReference(rowComponent, "m_LabelText", labelText);
            TrySetObjectReference(rowComponent, "m_ValueText", valueText);
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

        private static T GetRequiredComponent<T>(Transform transform) where T : Component
        {
            T component = transform.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException($"Component not found: {typeof(T).FullName} on {transform.name}");
            }

            return component;
        }

        private static T GetRequiredComponent<T>(Transform transform, Type componentType) where T : Component
        {
            Component component = transform.GetComponent(componentType);
            if (component == null)
            {
                throw new InvalidOperationException($"Component not found: {componentType.FullName} on {transform.name}");
            }

            return component as T;
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

        private static void TryGenerateMonoCodeBind(GameObject root)
        {
            MonoBehaviour formMonoBehaviour = root.GetComponent(ResolveType(MonoUIFormTypeName)) as MonoBehaviour;
            if (formMonoBehaviour == null)
            {
                throw new InvalidOperationException("MonoUIFormUnitAttribute MonoBehaviour not found.");
            }

            MonoBehaviour rowMonoBehaviour = FindRequiredChild(root.transform, "PlayerPanel_RectTransform/PlayerRows_RectTransform/PlayerItemTemplate_AttributeRowTemplate")
                .GetComponent(ResolveType(MonoRowTypeName)) as MonoBehaviour;
            if (rowMonoBehaviour == null)
            {
                throw new InvalidOperationException("MonoUIUnitAttributeRow MonoBehaviour not found.");
            }

            TryGenerateMonoCodeBind(formMonoBehaviour, root.transform);
            TryGenerateMonoCodeBind(rowMonoBehaviour, rowMonoBehaviour.transform);
        }

        private static void TryGenerateMonoCodeBind(MonoBehaviour monoBehaviour, Transform bindRoot)
        {
            MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (monoScript == null)
            {
                throw new InvalidOperationException($"MonoScript not found: {monoBehaviour.GetType().FullName}");
            }

            Type binderType = Type.GetType("CodeBind.Editor.MonoCodeBinder, CodeBind.Editor");
            if (binderType == null)
            {
                throw new InvalidOperationException("CodeBind.Editor.MonoCodeBinder type not found.");
            }

            object binder = Activator.CreateInstance(binderType, monoScript, bindRoot, '_');
            MethodInfo tryGenerateBindCodeMethod = binderType.GetMethod("TryGenerateBindCode", BindingFlags.Instance | BindingFlags.Public);
            if (tryGenerateBindCodeMethod == null)
            {
                throw new InvalidOperationException("MonoCodeBinder.TryGenerateBindCode not found.");
            }

            tryGenerateBindCodeMethod.Invoke(binder, null);
        }
    }
}
