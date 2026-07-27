using System;
using System.Reflection;
using Game;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ET.Editor
{
    public static class UIFormSkillsPrefabBuilder
    {
        private const string MenuPath = "ET/GenAtom/Rebuild UIFormSkills Items";
        private const string FormPrefabPath = "Assets/Res/UI/UIForm/GenAtom/UIFormSkills.prefab";
        private const string ItemPrefabPath = "Assets/Res/UI/UIPrefab/Skill/UISkillsItem.prefab";
        private const string MonoItemTypeName = "ET.Client.MonoUISkillsItem, Game.ET.Code.ModelView";
        private const string ChineseFontPath = "Assets/Res/Font/MaShanZheng-RegularSDF.asset";

        [MenuItem(MenuPath)]
        public static void Rebuild()
        {
            CreateItemPrefab();
            PatchFormPrefab();
            GenerateItemBindCode();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[UIFormSkillsPrefabBuilder] Rebuilt skill items: {FormPrefabPath}");
        }

        private static void CreateItemPrefab()
        {
            GameObject root = new GameObject(
                "UISkillsItem",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(LayoutElement),
                ResolveType(MonoItemTypeName));

            try
            {
                RectTransform rootRect = root.GetComponent<RectTransform>();
                rootRect.sizeDelta = new Vector2(112f, 128f);

                Image background = root.GetComponent<Image>();
                background.color = new Color(0.08f, 0.1f, 0.14f, 0.96f);
                background.raycastTarget = false;

                LayoutElement layoutElement = root.GetComponent<LayoutElement>();
                layoutElement.preferredWidth = 112f;
                layoutElement.preferredHeight = 128f;

                CreateImage("Icon_Image", root.transform, new Vector2(8f, 30f), new Vector2(-8f, -8f), Color.white, false);
                CreateText("Name_TextMeshProUGUI", root.transform, new Vector2(6f, 5f), new Vector2(-6f, 26f), 15f, TextAlignmentOptions.Center);
                CreateText("Level_TextMeshProUGUI", root.transform, new Vector2(5f, 102f), new Vector2(-5f, -5f), 14f, TextAlignmentOptions.BottomRight);
                CreateImage("Equipped_Image", root.transform, new Vector2(6f, 99f), new Vector2(27f, -8f), new Color(0.2f, 0.82f, 0.48f, 1f), false);

                GameObject clickObject = CreateImage(
                    "Click_ExButton",
                    root.transform,
                    Vector2.zero,
                    Vector2.zero,
                    new Color(1f, 1f, 1f, 0f),
                    true);
                ExButton button = clickObject.AddComponent<ExButton>();
                button.targetGraphic = clickObject.GetComponent<Image>();

                string directory = System.IO.Path.GetDirectoryName(ItemPrefabPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ItemPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Save prefab failed: {ItemPrefabPath}");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateImage(
            string name,
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            Color color,
            bool raycastTarget)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            image.preserveAspect = true;
            return imageObject;
        }

        private static void CreateText(
            string name,
            Transform parent,
            Vector2 offsetMin,
            Vector2 offsetMax,
            float fontSize,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChineseFontPath) ?? TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableAutoSizing = true;
            text.fontSizeMin = 10f;
            text.fontSizeMax = fontSize;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            text.color = Color.white;
        }

        private static void PatchFormPrefab()
        {
            GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPrefabPath);
            if (itemPrefab == null)
            {
                throw new InvalidOperationException($"Skill item prefab not found: {ItemPrefabPath}");
            }

            GameObject formRoot = PrefabUtility.LoadPrefabContents(FormPrefabPath);
            try
            {
                Transform contentRoot = FindRequiredChild(formRoot.transform, "Root");
                Transform legacyTemplate = contentRoot.Find("SkillTemp_ExButton_Image");
                if (legacyTemplate != null)
                {
                    legacyTemplate.gameObject.SetActive(false);
                }

                PatchLoop(contentRoot, "Skills_CommonLoopScrollRect", itemPrefab);
                PatchLoop(contentRoot, "Skill0_CommonLoopScrollRect", itemPrefab);
                PatchLoop(contentRoot, "Skill1_CommonLoopScrollRect", itemPrefab);

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(formRoot, FormPrefabPath);
                if (savedPrefab == null)
                {
                    throw new InvalidOperationException($"Save prefab failed: {FormPrefabPath}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(formRoot);
            }
        }

        private static void PatchLoop(Transform contentRoot, string loopName, GameObject itemPrefab)
        {
            Transform loopRoot = FindRequiredChild(contentRoot, loopName);
            Transform layout = FindRequiredChild(loopRoot, "Viewport/Layout");
            Transform oldTemplate = loopRoot.Find("ItemTemplate_UISkillsItem");
            if (oldTemplate != null)
            {
                UnityEngine.Object.DestroyImmediate(oldTemplate.gameObject);
            }

            oldTemplate = layout.Find("ItemTemplate_UISkillsItem");
            if (oldTemplate != null)
            {
                UnityEngine.Object.DestroyImmediate(oldTemplate.gameObject);
            }

            GameObject template = PrefabUtility.InstantiatePrefab(itemPrefab, loopRoot) as GameObject;
            if (template == null)
            {
                throw new InvalidOperationException($"Instantiate skill item failed: {loopName}");
            }

            template.name = "ItemTemplate_UISkillsItem";
            template.SetActive(true);

            GridLayoutGroup grid = layout.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(112f, 128f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            CommonLoopScrollRect loop = loopRoot.GetComponent<CommonLoopScrollRect>();
            SerializedObject serializedLoop = new SerializedObject(loop);
            serializedLoop.FindProperty("m_ItemTemplate").objectReferenceValue = template;
            serializedLoop.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void GenerateItemBindCode()
        {
            GameObject itemRoot = PrefabUtility.LoadPrefabContents(ItemPrefabPath);
            try
            {
                MonoBehaviour monoBehaviour = itemRoot.GetComponent(ResolveType(MonoItemTypeName)) as MonoBehaviour;
                if (monoBehaviour == null)
                {
                    throw new InvalidOperationException("MonoUISkillsItem component not found.");
                }

                Type binderType = Type.GetType("CodeBind.Editor.MonoCodeBinder, CodeBind.Editor");
                object binder = Activator.CreateInstance(binderType, MonoScript.FromMonoBehaviour(monoBehaviour), itemRoot.transform, '_');
                MethodInfo generateMethod = binderType.GetMethod("TryGenerateBindCode", BindingFlags.Instance | BindingFlags.Public);
                generateMethod.Invoke(binder, null);

                RefreshItemSerialization(monoBehaviour);
                PrefabUtility.SaveAsPrefabAsset(itemRoot, ItemPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(itemRoot);
            }
        }

        private static void RefreshItemSerialization(MonoBehaviour monoBehaviour)
        {
            SerializedObject serializedItem = new SerializedObject(monoBehaviour);
            SetReference(serializedItem, "m_ClickExButton", FindRequiredChild(monoBehaviour.transform, "Click_ExButton").GetComponent<ExButton>());
            SetReference(serializedItem, "m_EquippedImage", FindRequiredChild(monoBehaviour.transform, "Equipped_Image").GetComponent<Image>());
            SetReference(serializedItem, "m_IconImage", FindRequiredChild(monoBehaviour.transform, "Icon_Image").GetComponent<Image>());
            SetReference(serializedItem, "m_LevelTextMeshProUGUI", FindRequiredChild(monoBehaviour.transform, "Level_TextMeshProUGUI").GetComponent<TextMeshProUGUI>());
            SetReference(serializedItem, "m_NameTextMeshProUGUI", FindRequiredChild(monoBehaviour.transform, "Name_TextMeshProUGUI").GetComponent<TextMeshProUGUI>());
            serializedItem.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetReference(SerializedObject serializedObject, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
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
