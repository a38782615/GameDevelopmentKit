using System;
using System.IO;
using System.Reflection;
using Game;
using TMPro;
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

            RectTransform mapControlsRect = CreateMapControlsPanel(root.transform);
            CreateReloadSceneButton(mapControlsRect);
            CreateRerenderMapButton(mapControlsRect);

            RectTransform panelRect = CreatePanel(root.transform);
            RectTransform skillGridRect = CreateSkillGrid(panelRect);
            CreateSkillItemTemplate(skillGridRect);
            return root;
        }

        private static RectTransform CreateMapControlsPanel(Transform parent)
        {
            GameObject panel = new GameObject("MapControls", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 1f);
            rectTransform.anchorMax = new Vector2(0f, 1f);
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.anchoredPosition = new Vector2(24f, -24f);
            rectTransform.sizeDelta = new Vector2(420f, 268f);

            Image image = panel.GetComponent<Image>();
            image.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);

            TextMeshProUGUI title = CreateTMPText("Title", panel.transform, "Lake Controls", 24f, TextAlignmentOptions.Left);
            RectTransform titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -16f);
            titleRect.sizeDelta = new Vector2(-32f, 30f);
            title.color = new Color(0.95f, 0.97f, 1f, 0.98f);

            CreateMapControlRow(
                panel.transform,
                "LakeInlandMaskRow",
                "Inland Range",
                -60f,
                "LakeInlandMaskTight_Toggle",
                "Tight",
                false,
                "LakeInlandMaskDefault_Toggle",
                "Default",
                true,
                "LakeInlandMaskWide_Toggle",
                "Wide",
                false);

            CreateMapControlRow(
                panel.transform,
                "LakeCarveThresholdRow",
                "Carve Threshold",
                -116f,
                "LakeCarveThresholdSparse_Toggle",
                "Sparse",
                false,
                "LakeCarveThresholdDefault_Toggle",
                "Default",
                true,
                "LakeCarveThresholdDense_Toggle",
                "Dense",
                false);

            CreateMapControlRow(
                panel.transform,
                "LakeCarveStrengthRow",
                "Carve Strength",
                -172f,
                "LakeCarveStrengthShallow_Toggle",
                "Shallow",
                false,
                "LakeCarveStrengthDefault_Toggle",
                "Default",
                true,
                "LakeCarveStrengthDeep_Toggle",
                "Deep",
                false);

            return rectTransform;
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

        private static void CreateRerenderMapButton(Transform parent)
        {
            GameObject buttonObject = new GameObject(
                "RerenderMap_Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(1f, 0f);
            rectTransform.anchorMax = new Vector2(1f, 0f);
            rectTransform.pivot = new Vector2(1f, 0f);
            rectTransform.anchoredPosition = new Vector2(-24f, 24f);
            rectTransform.sizeDelta = new Vector2(190f, 52f);

            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.2f, 0.37f, 0.26f, 0.98f);

            Button button = buttonObject.GetComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = image.color;
            colors.highlightedColor = new Color(0.24f, 0.44f, 0.31f, 1f);
            colors.pressedColor = new Color(0.14f, 0.29f, 0.2f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.37f, 0.26f, 0.45f);
            button.colors = colors;

            TextMeshProUGUI label = CreateTMPText("Label", buttonObject.transform, "Apply + Render", 20f, TextAlignmentOptions.Center);
            RectTransform labelRectTransform = label.rectTransform;
            labelRectTransform.anchorMin = Vector2.zero;
            labelRectTransform.anchorMax = Vector2.one;
            labelRectTransform.offsetMin = new Vector2(10f, 6f);
            labelRectTransform.offsetMax = new Vector2(-10f, -6f);
            label.color = new Color(0.97f, 0.99f, 0.98f, 0.98f);
        }

        private static void CreateMapControlRow(
            Transform parent,
            string rowName,
            string label,
            float anchoredY,
            string option0Name,
            string option0Label,
            bool option0IsOn,
            string option1Name,
            string option1Label,
            bool option1IsOn,
            string option2Name,
            string option2Label,
            bool option2IsOn)
        {
            GameObject rowObject = new GameObject(rowName, typeof(RectTransform), typeof(ToggleGroup));
            rowObject.transform.SetParent(parent, false);

            RectTransform rowRect = rowObject.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0f, 1f);
            rowRect.anchorMax = new Vector2(1f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.anchoredPosition = new Vector2(0f, anchoredY);
            rowRect.sizeDelta = new Vector2(-28f, 44f);

            TextMeshProUGUI rowLabel = CreateTMPText("Label", rowObject.transform, label, 18f, TextAlignmentOptions.Left);
            RectTransform labelRect = rowLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = new Vector2(16f, 0f);
            labelRect.sizeDelta = new Vector2(128f, 0f);
            rowLabel.color = new Color(0.84f, 0.89f, 0.95f, 0.94f);

            ToggleGroup toggleGroup = rowObject.GetComponent<ToggleGroup>();
            toggleGroup.allowSwitchOff = false;

            CreateToggleOption(rowObject.transform, toggleGroup, option0Name, option0Label, new Vector2(174f, 0f), option0IsOn);
            CreateToggleOption(rowObject.transform, toggleGroup, option1Name, option1Label, new Vector2(260f, 0f), option1IsOn);
            CreateToggleOption(rowObject.transform, toggleGroup, option2Name, option2Label, new Vector2(346f, 0f), option2IsOn);
        }

        private static void CreateToggleOption(Transform parent, ToggleGroup group, string name, string label, Vector2 anchoredPosition, bool isOn)
        {
            GameObject toggleObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Toggle));
            toggleObject.transform.SetParent(parent, false);

            RectTransform rectTransform = toggleObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0f, 0.5f);
            rectTransform.anchorMax = new Vector2(0f, 0.5f);
            rectTransform.pivot = new Vector2(0f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(78f, 32f);

            Image background = toggleObject.GetComponent<Image>();
            background.color = new Color(0.12f, 0.17f, 0.23f, 0.98f);

            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggle.group = group;
            toggle.isOn = isOn;
            toggle.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = toggle.colors;
            colors.normalColor = background.color;
            colors.highlightedColor = new Color(0.17f, 0.24f, 0.31f, 1f);
            colors.pressedColor = new Color(0.11f, 0.16f, 0.21f, 1f);
            colors.selectedColor = new Color(0.23f, 0.33f, 0.42f, 1f);
            colors.disabledColor = new Color(0.12f, 0.17f, 0.23f, 0.45f);
            toggle.colors = colors;
            toggle.targetGraphic = background;

            GameObject indicatorObject = new GameObject("Indicator", typeof(RectTransform), typeof(Image));
            indicatorObject.transform.SetParent(toggleObject.transform, false);
            RectTransform indicatorRect = indicatorObject.GetComponent<RectTransform>();
            indicatorRect.anchorMin = new Vector2(0f, 0.5f);
            indicatorRect.anchorMax = new Vector2(0f, 0.5f);
            indicatorRect.pivot = new Vector2(0f, 0.5f);
            indicatorRect.anchoredPosition = new Vector2(8f, 0f);
            indicatorRect.sizeDelta = new Vector2(10f, 10f);
            Image indicator = indicatorObject.GetComponent<Image>();
            indicator.color = new Color(0.48f, 0.9f, 0.66f, 1f);
            indicator.raycastTarget = false;
            toggle.graphic = indicator;

            TextMeshProUGUI optionLabel = CreateTMPText("Label", toggleObject.transform, label, 16f, TextAlignmentOptions.Center);
            RectTransform labelRect = optionLabel.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);
            optionLabel.color = new Color(0.95f, 0.97f, 1f, 0.96f);
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

        private static TextMeshProUGUI CreateTMPText(string name, Transform parent, string text, float fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI textComponent = textObject.GetComponent<TextMeshProUGUI>();
            textComponent.text = text;
            textComponent.font = GetDefaultTMPFont();
            textComponent.fontSize = fontSize;
            textComponent.alignment = alignment;
            textComponent.enableWordWrapping = false;
            textComponent.overflowMode = TextOverflowModes.Ellipsis;
            textComponent.raycastTarget = false;
            return textComponent;
        }

        private static TMP_FontAsset GetDefaultTMPFont()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font != null)
            {
                return font;
            }

            font = AssetDatabase.GetBuiltinExtraResource<TMP_FontAsset>("Arial SDF.asset");
            if (font == null)
            {
                throw new InvalidOperationException("Default TMP font not found.");
            }

            return font;
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

            Transform mapControls = FindRequiredChild(root.transform, "MapControls");
            Button reloadSceneButton = GetRequiredComponent<Button>(FindRequiredChild(mapControls, "ReloadScene_Button"));
            Button rerenderMapButton = GetRequiredComponent<Button>(FindRequiredChild(mapControls, "RerenderMap_Button"));
            Toggle lakeInlandMaskTightToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeInlandMaskRow/LakeInlandMaskTight_Toggle"));
            Toggle lakeInlandMaskDefaultToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeInlandMaskRow/LakeInlandMaskDefault_Toggle"));
            Toggle lakeInlandMaskWideToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeInlandMaskRow/LakeInlandMaskWide_Toggle"));
            Toggle lakeCarveThresholdSparseToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveThresholdRow/LakeCarveThresholdSparse_Toggle"));
            Toggle lakeCarveThresholdDefaultToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveThresholdRow/LakeCarveThresholdDefault_Toggle"));
            Toggle lakeCarveThresholdDenseToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveThresholdRow/LakeCarveThresholdDense_Toggle"));
            Toggle lakeCarveStrengthShallowToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveStrengthRow/LakeCarveStrengthShallow_Toggle"));
            Toggle lakeCarveStrengthDefaultToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveStrengthRow/LakeCarveStrengthDefault_Toggle"));
            Toggle lakeCarveStrengthDeepToggle = GetRequiredComponent<Toggle>(FindRequiredChild(mapControls, "LakeCarveStrengthRow/LakeCarveStrengthDeep_Toggle"));
            RectTransform panelRectTransform = GetRequiredComponent<RectTransform>(FindRequiredChild(root.transform, "Panel_RectTransform"));
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

            TrySetObjectReference(formComponent, "m_ReloadSceneButton", reloadSceneButton);
            TrySetObjectReference(formComponent, "m_RerenderMapButton", rerenderMapButton);
            TrySetObjectReference(formComponent, "m_LakeInlandMaskTightToggle", lakeInlandMaskTightToggle);
            TrySetObjectReference(formComponent, "m_LakeInlandMaskDefaultToggle", lakeInlandMaskDefaultToggle);
            TrySetObjectReference(formComponent, "m_LakeInlandMaskWideToggle", lakeInlandMaskWideToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveThresholdSparseToggle", lakeCarveThresholdSparseToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveThresholdDefaultToggle", lakeCarveThresholdDefaultToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveThresholdDenseToggle", lakeCarveThresholdDenseToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveStrengthShallowToggle", lakeCarveStrengthShallowToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveStrengthDefaultToggle", lakeCarveStrengthDefaultToggle);
            TrySetObjectReference(formComponent, "m_LakeCarveStrengthDeepToggle", lakeCarveStrengthDeepToggle);
            TrySetObjectReference(formComponent, "m_PanelRectTransform", panelRectTransform);
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
