using System;
using System.IO;
using Game;
using UnityEditor;
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
        private const string LoopVerticalScrollRectTypeName = "UnityEngine.UI.LoopVerticalScrollRect, LoopScrollRect.Runtime";

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
            CreateTitle(panelRect);
            CreateCloseButton(panelRect);
            CreateSkillLoopScrollRect(panelRect);
            return root;
        }

        private static RectTransform CreatePanel(Transform parent)
        {
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.14f, 0.92f);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0.5f);
            rectTransform.anchorMax = new Vector2(1f, 0.5f);
            rectTransform.pivot = new Vector2(1f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(-24f, 0f);
            rectTransform.sizeDelta = new Vector2(420f, 460f);
            return rectTransform;
        }

        private static void CreateTitle(RectTransform parent)
        {
            Text titleText = CreateText("TitleText", parent, "Skills", 26, TextAnchor.MiddleLeft);
            RectTransform rectTransform = titleText.rectTransform;
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(20f, -18f);
            rectTransform.sizeDelta = new Vector2(220f, 40f);
            titleText.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        }

        private static void CreateCloseButton(RectTransform parent)
        {
            GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
            buttonObject.name = "CloseButton";
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(1f, 1f);
            rectTransform.anchoredPosition = new Vector2(-18f, -18f);
            rectTransform.sizeDelta = new Vector2(96f, 40f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.76f, 0.24f, 0.24f, 1f);

            Text buttonText = buttonObject.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = "Close";
                buttonText.fontSize = 20;
                buttonText.color = Color.white;
            }
        }

        private static void CreateSkillLoopScrollRect(RectTransform parent)
        {
            GameObject scrollObject = DefaultControls.CreateScrollView(new DefaultControls.Resources());
            scrollObject.name = "SkillLoopScrollRect";
            scrollObject.transform.SetParent(parent, false);

            RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0f, 0f);
            scrollRectTransform.anchorMax = new Vector2(1f, 1f);
            scrollRectTransform.offsetMin = new Vector2(18f, 18f);
            scrollRectTransform.offsetMax = new Vector2(-18f, -70f);

            Image scrollBackground = scrollObject.GetComponent<Image>();
            scrollBackground.color = new Color(0.12f, 0.15f, 0.2f, 0.95f);

            Transform horizontalScrollbar = scrollObject.transform.Find("Scrollbar Horizontal");
            if (horizontalScrollbar != null)
            {
                UnityEngine.Object.DestroyImmediate(horizontalScrollbar.gameObject);
            }

            Transform verticalScrollbar = scrollObject.transform.Find("Scrollbar Vertical");
            if (verticalScrollbar != null)
            {
                UnityEngine.Object.DestroyImmediate(verticalScrollbar.gameObject);
            }

            RectTransform viewport = scrollObject.transform.Find("Viewport") as RectTransform;
            RectTransform content = viewport?.Find("Content") as RectTransform;
            if (viewport == null || content == null)
            {
                return;
            }

            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;

            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.color = new Color(1f, 1f, 1f, 0.04f);
            }

            Mask mask = viewport.GetComponent<Mask>();
            if (mask != null)
            {
                mask.showMaskGraphic = false;
            }

            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layoutGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 10f;
            layoutGroup.padding = new RectOffset(12, 12, 12, 12);

            CreateSkillItemTemplate(content);

            ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                UnityEngine.Object.DestroyImmediate(scrollRect);
            }

            Component loopScrollRect = scrollObject.AddComponent(ResolveType(LoopVerticalScrollRectTypeName));
            SetProperty(loopScrollRect, "content", content);
            SetProperty(loopScrollRect, "viewport", viewport);
            SetProperty(loopScrollRect, "horizontal", false);
            SetProperty(loopScrollRect, "vertical", true);
            SetEnumProperty(loopScrollRect, "movementType", "Clamped");
            SetProperty(loopScrollRect, "inertia", true);
            SetProperty(loopScrollRect, "scrollSensitivity", 36f);
            SetProperty(loopScrollRect, "decelerationRate", 0.135f);

            scrollObject.AddComponent<CommonLoopScrollRect>();
        }

        private static void CreateSkillItemTemplate(RectTransform parent)
        {
            GameObject item = new GameObject(
                "SkillItemTemplate",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));

            item.AddComponent(ResolveType(MonoUISkillItemTypeName));

            item.transform.SetParent(parent, false);

            RectTransform rectTransform = item.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(1f, 1f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.sizeDelta = new Vector2(0f, 72f);

            Image image = item.GetComponent<Image>();
            image.color = new Color(0.18f, 0.22f, 0.29f, 1f);

            LayoutElement layoutElement = item.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 72f;
            layoutElement.minHeight = 72f;

            HorizontalLayoutGroup horizontalLayoutGroup = item.GetComponent<HorizontalLayoutGroup>();
            horizontalLayoutGroup.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayoutGroup.childControlWidth = true;
            horizontalLayoutGroup.childControlHeight = true;
            horizontalLayoutGroup.childForceExpandWidth = false;
            horizontalLayoutGroup.childForceExpandHeight = false;
            horizontalLayoutGroup.spacing = 12f;
            horizontalLayoutGroup.padding = new RectOffset(12, 12, 10, 10);

            CreateIcon(item.transform);
            CreateNameText(item.transform);
            CreateStateText(item.transform);
        }

        private static void CreateIcon(Transform parent)
        {
            GameObject icon = new GameObject("IconImage", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            icon.transform.SetParent(parent, false);

            Image image = icon.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.95f);
            image.enabled = false;

            LayoutElement layoutElement = icon.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 44f;
            layoutElement.preferredHeight = 44f;
            layoutElement.minWidth = 44f;
            layoutElement.minHeight = 44f;
        }

        private static void CreateNameText(Transform parent)
        {
            Text nameText = CreateText("NameText", parent, "Skill", 22, TextAnchor.MiddleLeft);
            LayoutElement layoutElement = nameText.gameObject.AddComponent<LayoutElement>();
            layoutElement.flexibleWidth = 1f;
            layoutElement.minWidth = 120f;
            nameText.color = new Color(0.94f, 0.95f, 0.98f, 1f);
        }

        private static void CreateStateText(Transform parent)
        {
            Text stateText = CreateText("StateText", parent, "Ready", 20, TextAnchor.MiddleRight);
            LayoutElement layoutElement = stateText.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 96f;
            layoutElement.minWidth = 96f;
            stateText.color = new Color(0.54f, 0.83f, 0.98f, 1f);
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
            textComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
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

        private static Type ResolveType(string assemblyQualifiedTypeName)
        {
            Type type = Type.GetType(assemblyQualifiedTypeName);
            if (type == null)
            {
                throw new InvalidOperationException($"Type not found: {assemblyQualifiedTypeName}");
            }

            return type;
        }

        private static void SetProperty(Component component, string propertyName, object value)
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property not found: {component.GetType().FullName}.{propertyName}");
            }

            property.SetValue(component, value);
        }

        private static void SetEnumProperty(Component component, string propertyName, string value)
        {
            var property = component.GetType().GetProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException($"Property not found: {component.GetType().FullName}.{propertyName}");
            }

            object enumValue = Enum.Parse(property.PropertyType, value);
            property.SetValue(component, enumValue);
        }
    }
}
