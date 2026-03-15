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
        private const string DefaultTmpFontAssetPath = "Assets/Res/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string TextMeshProUGUITypeName = "TMPro.TextMeshProUGUI, Unity.TextMeshPro";
        private const string FontStylesTypeName = "TMPro.FontStyles, Unity.TextMeshPro";
        private const string TextAlignmentOptionsTypeName = "TMPro.TextAlignmentOptions, Unity.TextMeshPro";
        private const string TextOverflowModesTypeName = "TMPro.TextOverflowModes, Unity.TextMeshPro";
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
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Save prefab failed: {PrefabPath}");
                }

                ScheduleSavedPrefabNormalization();
                TryGenerateMonoCodeBind(root);
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

            Component titleText = CreateText($"{prefix}Title_Text", panel.transform, isPlayerPanel ? "Player" : "Monster", 24, "Bold", "MidlineLeft");
            SetGraphicColor(titleText, accentColor);
            SetLayoutElement(titleText.gameObject, 34f, 0f, 0f);

            Component tagsText = CreateText($"{prefix}Tags_Text", panel.transform, "Tags: None", 16, "Normal", "TopLeft");
            SetGraphicColor(tagsText, new Color(0.83f, 0.88f, 0.95f, 0.92f));
            SetObjectMember(tagsText, "enableWordWrapping", true);
            SetEnumMember(tagsText, "overflowMode", TextOverflowModesTypeName, "Overflow");
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

            Component categoryTemplate = CreateText($"{prefix}CategoryTemplate_Text", rowsRoot.transform, "Category", 18, "Bold", "MidlineLeft");
            SetGraphicColor(categoryTemplate, accentColor);
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

            Component labelText = CreateText("Label_Text", row.transform, "Label", 16, "Normal", "MidlineLeft");
            SetGraphicColor(labelText, new Color(0.95f, 0.97f, 1f, 0.94f));
            LayoutElement labelLayout = labelText.gameObject.AddComponent<LayoutElement>();
            labelLayout.preferredWidth = 150f;
            labelLayout.flexibleWidth = 1f;

            Component valueText = CreateText("Value_Text", row.transform, "0", 16, "Bold", "MidlineRight");
            SetGraphicColor(valueText, new Color(1f, 0.89f, 0.48f, 0.98f));
            LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 110f;
            valueLayout.flexibleWidth = 0f;

            row.SetActive(false);
        }

        private static Component CreateText(
            string name,
            Transform parent,
            string text,
            int fontSize,
            string fontStyle,
            string anchor)
        {
            Type textType = ResolveType(TextMeshProUGUITypeName);
            GameObject textObject = new GameObject(name, typeof(RectTransform), textType);
            textObject.transform.SetParent(parent, false);

            Component textComponent = textObject.GetComponent(textType);
            SetObjectMember(textComponent, "text", text);
            SetObjectMember(textComponent, "font", GetDefaultTmpFontAsset());
            SetObjectMember(textComponent, "fontSize", (float)fontSize);
            SetEnumMember(textComponent, "fontStyle", FontStylesTypeName, fontStyle);
            SetEnumMember(textComponent, "alignment", TextAlignmentOptionsTypeName, anchor);
            SetObjectMember(textComponent, "enableWordWrapping", false);
            SetEnumMember(textComponent, "overflowMode", TextOverflowModesTypeName, "Overflow");
            SetObjectMember(textComponent, "raycastTarget", false);

            RectTransform rectTransform = ((RectTransform)textObject.transform);
            rectTransform.localScale = Vector3.one;
            return textComponent;
        }

        private static UnityEngine.Object GetDefaultTmpFontAsset()
        {
            UnityEngine.Object fontAsset = AssetDatabase.LoadMainAssetAtPath(DefaultTmpFontAssetPath);
            if (fontAsset != null)
            {
                return fontAsset;
            }

            throw new InvalidOperationException("TMP default font asset not found.");
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
            Component playerTitleText = GetRequiredComponent<Component>(FindRequiredChild(playerPanelRectTransform, "PlayerTitle_Text"), ResolveType(TextMeshProUGUITypeName));
            Component playerTagsText = GetRequiredComponent<Component>(FindRequiredChild(playerPanelRectTransform, "PlayerTags_Text"), ResolveType(TextMeshProUGUITypeName));
            RectTransform playerRowsRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(playerPanelRectTransform, "PlayerRows_RectTransform"));
            Component playerCategoryTemplateText = GetRequiredComponent<Component>(FindRequiredChild(playerRowsRectTransform, "PlayerCategoryTemplate_Text"), ResolveType(TextMeshProUGUITypeName));
            Component playerItemTemplate = GetRequiredComponent<Component>(FindRequiredChild(playerRowsRectTransform, "PlayerItemTemplate_AttributeRowTemplate"), ResolveType(MonoRowTypeName));

            RectTransform monsterPanelRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(root.transform, "MonsterPanel_RectTransform"));
            Component monsterTitleText = GetRequiredComponent<Component>(FindRequiredChild(monsterPanelRectTransform, "MonsterTitle_Text"), ResolveType(TextMeshProUGUITypeName));
            Component monsterTagsText = GetRequiredComponent<Component>(FindRequiredChild(monsterPanelRectTransform, "MonsterTags_Text"), ResolveType(TextMeshProUGUITypeName));
            RectTransform monsterRowsRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(monsterPanelRectTransform, "MonsterRows_RectTransform"));
            Component monsterCategoryTemplateText = GetRequiredComponent<Component>(FindRequiredChild(monsterRowsRectTransform, "MonsterCategoryTemplate_Text"), ResolveType(TextMeshProUGUITypeName));
            Component monsterItemTemplate = GetRequiredComponent<Component>(FindRequiredChild(monsterRowsRectTransform, "MonsterItemTemplate_AttributeRowTemplate"), ResolveType(MonoRowTypeName));

            TrySetObjectReference(formComponent, "m_PlayerPanelRectTransform", playerPanelRectTransform);
            TrySetObjectReference(formComponent, "m_PlayerTitleTextMeshProUGUI", playerTitleText);
            TrySetObjectReference(formComponent, "m_PlayerTagsTextMeshProUGUI", playerTagsText);
            TrySetObjectReference(formComponent, "m_PlayerRowsRectTransform", playerRowsRectTransform);
            TrySetObjectReference(formComponent, "m_PlayerCategoryTemplateTextMeshProUGUI", playerCategoryTemplateText);
            TrySetObjectReference(formComponent, "m_PlayerItemTemplateAttributeRowTemplate", playerItemTemplate);

            TrySetObjectReference(formComponent, "m_MonsterPanelRectTransform", monsterPanelRectTransform);
            TrySetObjectReference(formComponent, "m_MonsterTitleTextMeshProUGUI", monsterTitleText);
            TrySetObjectReference(formComponent, "m_MonsterTagsTextMeshProUGUI", monsterTagsText);
            TrySetObjectReference(formComponent, "m_MonsterRowsRectTransform", monsterRowsRectTransform);
            TrySetObjectReference(formComponent, "m_MonsterCategoryTemplateTextMeshProUGUI", monsterCategoryTemplateText);
            TrySetObjectReference(formComponent, "m_MonsterItemTemplateAttributeRowTemplate", monsterItemTemplate);

            RefreshRowBind(playerItemTemplate);
            RefreshRowBind(monsterItemTemplate);
        }

        private static void RefreshRowBind(Component rowComponent)
        {
            Component labelText = GetRequiredComponent<Component>(FindRequiredChild(rowComponent.transform, "Label_Text"), ResolveType(TextMeshProUGUITypeName));
            Component valueText = GetRequiredComponent<Component>(FindRequiredChild(rowComponent.transform, "Value_Text"), ResolveType(TextMeshProUGUITypeName));

            TrySetObjectReference(rowComponent, "m_LabelTextMeshProUGUI", labelText);
            TrySetObjectReference(rowComponent, "m_ValueTextMeshProUGUI", valueText);
        }

        private static Transform FindRequiredChild(Transform parent, string path)
        {
            Transform child = parent.Find(path);
            if (child == null)
            {
                string[] segments = path.Split('/');
                child = FindChildRecursive(parent, segments, 0);
            }

            if (child == null)
            {
                throw new InvalidOperationException($"Child not found: {path}");
            }

            return child;
        }

        private static Transform FindChildRecursive(Transform parent, string[] segments, int index)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; ++i)
            {
                Transform child = parent.GetChild(i);
                if (child.name != segments[index])
                {
                    continue;
                }

                if (index == segments.Length - 1)
                {
                    return child;
                }

                Transform nestedChild = FindChildRecursive(child, segments, index + 1);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            if (index != 0)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; ++i)
            {
                Transform nestedChild = FindChildRecursive(parent.GetChild(i), segments, index);
                if (nestedChild != null)
                {
                    return nestedChild;
                }
            }

            return null;
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

        private static void SetGraphicColor(Component component, Color color)
        {
            Graphic graphic = component as Graphic;
            if (graphic == null)
            {
                throw new InvalidOperationException($"Graphic component expected on {component?.name ?? "null"}.");
            }

            graphic.color = color;
        }

        private static void SetObjectMember(Component component, string memberName, object value)
        {
            PropertyInfo property = component.GetType().GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property != null && property.CanWrite)
            {
                property.SetValue(component, value);
                return;
            }

            FieldInfo field = component.GetType().GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(component, value);
                return;
            }

            throw new InvalidOperationException($"Writable member not found: {component.GetType().FullName}.{memberName}");
        }

        private static void SetEnumMember(Component component, string memberName, string enumTypeName, string enumValueName)
        {
            Type enumType = ResolveType(enumTypeName);
            object enumValue = Enum.Parse(enumType, enumValueName);
            SetObjectMember(component, memberName, enumValue);
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
