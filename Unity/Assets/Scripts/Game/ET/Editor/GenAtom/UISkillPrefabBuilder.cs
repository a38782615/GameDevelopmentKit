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
    public static class UISkillPrefabBuilder
    {
        private const string MenuPath = "ET/GenAtom/Rebuild UISkill Prefab";
        private const string PrefabPath = "Assets/Res/UI/UIForm/GenAtom/UISkill.prefab";
        private const string MonoUIFormSkillTypeName = "ET.Client.MonoUIFormSkill, Game.ET.Code.ModelView";
        private const string MonoUISkillItemTypeName = "ET.Client.MonoUISkillItem, Game.ET.Code.ModelView";
        private const string PendingNormalizeKey = "UISkillPrefabBuilder.PendingNormalize";
        private const string CooldownSectorSpritePath = "Assets/Res/UI/UISprite/Common/circle-filled.png";
        private static readonly Vector2 iconInset = new Vector2(12f, 12f);

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
                Debug.Log($"[UISkillPrefabBuilder] Rebuilt prefab: {PrefabPath}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [MenuItem("ET/GenAtom/Finalize UISkill Prefab")]
        public static void FinalizePrefab()
        {
            NormalizeSavedPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UISkillPrefabBuilder] Finalized prefab: {PrefabPath}");
        }

        private static GameObject CreateRoot()
        {
            GameObject root = new GameObject(
                "UISkill",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasRenderer),
                typeof(GraphicRaycaster),
                typeof(RaycastGraphic));

            root.AddComponent(ResolveType(MonoUIFormSkillTypeName));

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
            raycastGraphic.raycastTarget = true;

            RectTransform panelRect = CreatePanel(root.transform);
            RectTransform skillGridRect = CreateSkillGrid(panelRect);
            CreateSkillItemTemplate(skillGridRect);
            CreateReloadSceneButton(root.transform);
            return root;
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("Panel_RectTransform", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.05f, 0.08f, 0.12f, 0.86f);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0f);
            rectTransform.anchorMax = new Vector2(0.5f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = new Vector2(0f, 20f);
            rectTransform.sizeDelta = new Vector2(820f, 196f);
            return rectTransform;
        }

        private static RectTransform CreateSkillGrid(RectTransform parent)
        {
            GameObject skillGrid = new GameObject("SkillGrid_RectTransform_GridLayoutGroup", typeof(RectTransform), typeof(GridLayoutGroup));
            skillGrid.transform.SetParent(parent, false);

            RectTransform rectTransform = skillGrid.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(18f, 18f);
            rectTransform.offsetMax = new Vector2(-18f, -18f);

            GridLayoutGroup gridLayoutGroup = skillGrid.GetComponent<GridLayoutGroup>();
            gridLayoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            gridLayoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = 1;
            gridLayoutGroup.cellSize = new Vector2(160f, 160f);
            gridLayoutGroup.spacing = new Vector2(20f, 20f);

            return rectTransform;
        }

        private static void CreateSkillItemTemplate(RectTransform parent)
        {
            GameObject item = new GameObject(
                "ItemTemplate_SkillItemTemplate",
                typeof(RectTransform),
                ResolveType(MonoUISkillItemTypeName));

            item.transform.SetParent(parent, false);

            RectTransform rectTransform = item.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(160f, 160f);

            GameObject castButtonObject = new GameObject(
                "Cast_Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            castButtonObject.transform.SetParent(item.transform, false);

            RectTransform castButtonRect = castButtonObject.GetComponent<RectTransform>();
            castButtonRect.anchorMin = Vector2.zero;
            castButtonRect.anchorMax = Vector2.one;
            castButtonRect.offsetMin = Vector2.zero;
            castButtonRect.offsetMax = Vector2.zero;

            Image castButtonImage = castButtonObject.GetComponent<Image>();
            castButtonImage.color = new Color(0.16f, 0.2f, 0.27f, 0.96f);

            CreateIcon(castButtonObject.transform);
            CreateCooldownTrack(castButtonObject.transform);
            CreateCooldownRing(castButtonObject.transform);
            CreateNameText(castButtonObject.transform);
            CreateStateText(castButtonObject.transform);
            item.SetActive(false);
        }

        private static void CreateReloadSceneButton(Transform parent)
        {
            GameObject buttonObject = new GameObject(
                "ReloadScene_Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(0f, 0f);
            rectTransform.pivot = new Vector2(0f, 0f);
            rectTransform.anchoredPosition = new Vector2(24f, 24f);
            rectTransform.sizeDelta = new Vector2(180f, 52f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.14f, 0.19f, 0.24f, 0.95f);

            Text label = CreateText("Label", buttonObject.transform, "重新加载场景", 24, TextAnchor.MiddleCenter);
            RectTransform labelRectTransform = label.rectTransform;
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = new Vector2(10f, 6f);
            labelRectTransform.offsetMax = new Vector2(-10f, -6f);
            label.color = new Color(0.95f, 0.97f, 1f, 0.95f);
        }

        private static void CreateIcon(Transform parent)
        {
            GameObject icon = new GameObject("Icon_Image", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(parent, false);

            RectTransform rectTransform = icon.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = iconInset;
            rectTransform.offsetMax = -iconInset;

            Image image = icon.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);
            image.enabled = false;
            image.preserveAspect = true;
        }

        private static void CreateCooldownTrack(Transform parent)
        {
            GameObject cooldownTrack = new GameObject("CooldownTrack_Image", typeof(RectTransform), typeof(Image));
            cooldownTrack.transform.SetParent(parent, false);

            RectTransform rectTransform = cooldownTrack.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = iconInset;
            rectTransform.offsetMax = -iconInset;

            Image image = cooldownTrack.GetComponent<Image>();
            image.sprite = LoadSprite(CooldownSectorSpritePath);
            image.type = Image.Type.Simple;
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            cooldownTrack.SetActive(false);
        }

        private static void CreateCooldownRing(Transform parent)
        {
            GameObject cooldownRing = new GameObject("CooldownRing_Image", typeof(RectTransform), typeof(Image));
            cooldownRing.transform.SetParent(parent, false);

            RectTransform rectTransform = cooldownRing.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = iconInset;
            rectTransform.offsetMax = -iconInset;

            Image image = cooldownRing.GetComponent<Image>();
            image.sprite = LoadSprite(CooldownSectorSpritePath);
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Radial360;
            image.fillOrigin = (int)Image.Origin360.Top;
            image.fillClockwise = false;
            image.fillAmount = 0f;
            image.color = new Color(0f, 0f, 0f, 0.62f);
            image.raycastTarget = false;
            image.preserveAspect = true;
            cooldownRing.SetActive(false);
        }

        private static void CreateNameText(Transform parent)
        {
            Text nameText = CreateText("Name_Text", parent, "Skill", 24, TextAnchor.LowerCenter);
            RectTransform rectTransform = nameText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(0.5f, 0f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(0f, 36f);
            nameText.color = new Color(0.94f, 0.95f, 0.98f, 0.92f);
            nameText.gameObject.SetActive(false);
        }

        private static void CreateStateText(Transform parent)
        {
            Text stateText = CreateText("State_Text", parent, string.Empty, 32, TextAnchor.MiddleCenter);
            RectTransform rectTransform = stateText.rectTransform;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = new Vector2(24f, 24f);
            rectTransform.offsetMax = new Vector2(-24f, -24f);
            stateText.color = new Color(0.97f, 0.98f, 1f, 1f);
            Outline outline = stateText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(2.4f, -2.4f);
            stateText.gameObject.SetActive(false);
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor anchor)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text textComponent = textObject.GetComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.alignment = anchor;
            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;
            textComponent.raycastTarget = false;
            return textComponent;
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
            Component formComponent = root.GetComponent(ResolveType(MonoUIFormSkillTypeName));
            if (formComponent == null)
            {
                throw new InvalidOperationException("MonoUIFormSkill component not found.");
            }

            RectTransform panelRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(root.transform, "Panel_RectTransform"));
            Button reloadSceneButton = GetRequiredComponent<Button>(FindRequiredChild(root.transform, "ReloadScene_Button"));
            Transform skillGrid = FindRequiredChild(panelRectTransform, "SkillGrid_RectTransform_GridLayoutGroup");
            RectTransform skillGridRectTransform = GetRequiredComponent<RectTransform>(skillGrid);
            GridLayoutGroup skillGridLayoutGroup = GetRequiredComponent<GridLayoutGroup>(skillGrid);
            Transform itemRoot = FindRequiredChild(skillGrid, "ItemTemplate_SkillItemTemplate");

            Component itemComponent = itemRoot.GetComponent(ResolveType(MonoUISkillItemTypeName));
            if (itemComponent == null)
            {
                throw new InvalidOperationException("MonoUISkillItem component not found.");
            }

            Button castButton = GetRequiredComponent<Button>(FindRequiredChild(itemRoot, "Cast_Button"));
            Transform castButtonTransform = castButton.transform;
            Image iconImage = GetRequiredComponent<Image>(FindRequiredChild(castButtonTransform, "Icon_Image"));
            Image cooldownRingImage = GetRequiredComponent<Image>(FindRequiredChild(castButtonTransform, "CooldownRing_Image"));
            Image cooldownTrackImage = GetRequiredComponent<Image>(FindRequiredChild(castButtonTransform, "CooldownTrack_Image"));
            Text nameText = GetRequiredComponent<Text>(FindRequiredChild(castButtonTransform, "Name_Text"));
            Text stateText = GetRequiredComponent<Text>(FindRequiredChild(castButtonTransform, "State_Text"));

            TrySetObjectReference(formComponent, "m_PanelRectTransform", panelRectTransform);
            TrySetObjectReference(formComponent, "m_ReloadSceneButton", reloadSceneButton);
            TrySetObjectReference(formComponent, "m_SkillGridRectTransform", skillGridRectTransform);
            TrySetObjectReference(formComponent, "m_SkillGridGridLayoutGroup", skillGridLayoutGroup);
            TrySetObjectReference(formComponent, "m_ItemTemplateSkillItemTemplate", itemComponent);

            TrySetObjectReference(itemComponent, "m_CastButton", castButton);
            TrySetObjectReference(itemComponent, "m_CooldownTrackImage", cooldownTrackImage);
            TrySetObjectReference(itemComponent, "m_IconImage", iconImage);
            TrySetObjectReference(itemComponent, "m_CooldownRingImage", cooldownRingImage);
            TrySetObjectReference(itemComponent, "m_NameText", nameText);
            TrySetObjectReference(itemComponent, "m_StateText", stateText);
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
            MonoBehaviour monoBehaviour = root.GetComponent(ResolveType(MonoUIFormSkillTypeName)) as MonoBehaviour;
            if (monoBehaviour == null)
            {
                throw new InvalidOperationException("MonoUIFormSkill MonoBehaviour not found.");
            }

            MonoScript monoScript = MonoScript.FromMonoBehaviour(monoBehaviour);
            if (monoScript == null)
            {
                throw new InvalidOperationException("MonoUIFormSkill MonoScript not found.");
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

        private static Sprite LoadSprite(string assetPath)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Sprite not found: {assetPath}");
            }

            return sprite;
        }
    }
}
