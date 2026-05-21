using GameFramework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    [CustomEditor(typeof(TextMeshProEffectController))]
    public sealed class TextMeshProEffectControllerEditor : UnityEditor.Editor
    {
        private SerializedProperty m_TextMeshProUGUIProperty;
        private SerializedProperty m_RuntimeMaterialProperty;
        private SerializedProperty m_SourceMaterialProperty;
        private SerializedProperty m_OutlineProperty;
        private SerializedProperty m_GlowProperty;
        private SerializedProperty m_ShadowProperty;

        private void OnEnable()
        {
            m_TextMeshProUGUIProperty = serializedObject.FindProperty("m_TextMeshProUGUI");
            m_RuntimeMaterialProperty = serializedObject.FindProperty("m_RuntimeMaterial");
            m_SourceMaterialProperty = serializedObject.FindProperty("m_SourceMaterial");
            m_OutlineProperty = serializedObject.FindProperty("m_Outline");
            m_GlowProperty = serializedObject.FindProperty("m_Glow");
            m_ShadowProperty = serializedObject.FindProperty("m_Shadow");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_TextMeshProUGUIProperty);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_RuntimeMaterialProperty);
                EditorGUILayout.PropertyField(m_SourceMaterialProperty);
            }

            EditorGUILayout.Space();
            DrawOutline();
            EditorGUILayout.Space();
            DrawGlow();
            EditorGUILayout.Space();
            DrawShadow();
            EditorGUILayout.Space();

            if (GUILayout.Button("Apply Effects"))
            {
                ApplyEffectsToTarget((TextMeshProEffectController)target, true);
            }

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                ApplyEffectsToTarget((TextMeshProEffectController)target, false);
            }
        }

        private void DrawOutline()
        {
            SerializedProperty enabledProperty = m_OutlineProperty.FindPropertyRelative("Enabled");
            SerializedProperty widthProperty = m_OutlineProperty.FindPropertyRelative("Width");
            SerializedProperty colorProperty = m_OutlineProperty.FindPropertyRelative("Color");

            EditorGUILayout.LabelField("Outline", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enabledProperty, new GUIContent("Enabled"));
            using (new EditorGUI.DisabledScope(!enabledProperty.boolValue))
            {
                EditorGUILayout.Slider(widthProperty, 0f, 1f, new GUIContent("Width"));
                EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color"));
            }
        }

        private void DrawGlow()
        {
            SerializedProperty enabledProperty = m_GlowProperty.FindPropertyRelative("Enabled");
            SerializedProperty colorProperty = m_GlowProperty.FindPropertyRelative("Color");
            SerializedProperty offsetProperty = m_GlowProperty.FindPropertyRelative("Offset");
            SerializedProperty powerProperty = m_GlowProperty.FindPropertyRelative("Power");

            EditorGUILayout.LabelField("Glow", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enabledProperty, new GUIContent("Enabled"));
            using (new EditorGUI.DisabledScope(!enabledProperty.boolValue))
            {
                EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color"));
                EditorGUILayout.Slider(offsetProperty, 0f, 1f, new GUIContent("Offset"));
                EditorGUILayout.Slider(powerProperty, 0f, 1f, new GUIContent("Power"));
            }
        }

        private void DrawShadow()
        {
            SerializedProperty enabledProperty = m_ShadowProperty.FindPropertyRelative("Enabled");
            SerializedProperty colorProperty = m_ShadowProperty.FindPropertyRelative("Color");
            SerializedProperty offsetXProperty = m_ShadowProperty.FindPropertyRelative("OffsetX");
            SerializedProperty offsetYProperty = m_ShadowProperty.FindPropertyRelative("OffsetY");
            SerializedProperty dilateProperty = m_ShadowProperty.FindPropertyRelative("Dilate");
            SerializedProperty softnessProperty = m_ShadowProperty.FindPropertyRelative("Softness");

            EditorGUILayout.LabelField("Shadow", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enabledProperty, new GUIContent("Enabled"));
            using (new EditorGUI.DisabledScope(!enabledProperty.boolValue))
            {
                EditorGUILayout.PropertyField(colorProperty, new GUIContent("Color"));
                EditorGUILayout.Slider(offsetXProperty, -1f, 1f, new GUIContent("Offset X"));
                EditorGUILayout.Slider(offsetYProperty, -1f, 1f, new GUIContent("Offset Y"));
                EditorGUILayout.Slider(dilateProperty, -1f, 1f, new GUIContent("Dilate"));
                EditorGUILayout.Slider(softnessProperty, 0f, 1f, new GUIContent("Softness"));
            }
        }

        private static void ApplyEffectsToTarget(TextMeshProEffectController controller, bool pingMaterial)
        {
            if (controller == null)
            {
                return;
            }

            TextMeshProUGUI textMeshProUGUI = controller.TextMeshProUGUI != null
                ? controller.TextMeshProUGUI
                : controller.GetComponent<TextMeshProUGUI>();
            if (textMeshProUGUI == null)
            {
                Debug.LogError("TextMeshProEffectController requires a TextMeshProUGUI component.");
                return;
            }

            controller.CaptureSourceMaterial();
            Material sourceMaterial = controller.SourceMaterial != null
                ? controller.SourceMaterial
                : textMeshProUGUI.fontSharedMaterial;
            if (sourceMaterial == null)
            {
                Debug.LogError("Current TextMeshProUGUI has no source material.");
                return;
            }

            Material runtimeMaterial = EnsureRuntimeMaterial(controller, sourceMaterial);
            if (runtimeMaterial == null)
            {
                Debug.LogError("Failed to create TextMeshPro effect material.");
                return;
            }

            ApplyOutline(runtimeMaterial, controller.Outline);
            ApplyGlow(runtimeMaterial, controller.Glow);
            ApplyShadow(runtimeMaterial, controller.Shadow);

            controller.ApplyToText();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(textMeshProUGUI);
            EditorUtility.SetDirty(runtimeMaterial);

            if (pingMaterial)
            {
                EditorGUIUtility.PingObject(runtimeMaterial);
            }
        }

        private static Material EnsureRuntimeMaterial(TextMeshProEffectController controller, Material sourceMaterial)
        {
            Material runtimeMaterial = controller.RuntimeMaterial;
            string ownerAssetPath = GetOwnerAssetPath(controller.gameObject);
            if (string.IsNullOrEmpty(ownerAssetPath))
            {
                return null;
            }

            if (runtimeMaterial != null && AssetDatabase.Contains(runtimeMaterial))
            {
                EditorUtility.CopySerialized(sourceMaterial, runtimeMaterial);
                runtimeMaterial.name = GetRuntimeMaterialName(controller);
                controller.SetRuntimeMaterial(runtimeMaterial);
                return runtimeMaterial;
            }

            runtimeMaterial = new Material(sourceMaterial)
            {
                name = GetRuntimeMaterialName(controller)
            };
            AssetDatabase.AddObjectToAsset(runtimeMaterial, ownerAssetPath);
            controller.SetRuntimeMaterial(runtimeMaterial);
            return runtimeMaterial;
        }

        private static string GetOwnerAssetPath(GameObject gameObject)
        {
            string prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(gameObject);
            if (!string.IsNullOrEmpty(prefabAssetPath))
            {
                return prefabAssetPath;
            }

            GameObject sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (sourceObject == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(sourceObject);
        }

        private static string GetRuntimeMaterialName(TextMeshProEffectController controller)
        {
            return Utility.Text.Format("{0}_TMPEffectMaterial", controller.gameObject.name);
        }

        private static void ApplyOutline(Material material, TextMeshProEffectController.OutlineSettings settings)
        {
            if (material.HasProperty(ShaderUtilities.ID_OutlineWidth))
            {
                material.SetFloat(ShaderUtilities.ID_OutlineWidth, settings.Enabled ? settings.Width : 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_OutlineColor))
            {
                Color color = settings.Color;
                color.a = settings.Enabled ? color.a : 0f;
                material.SetColor(ShaderUtilities.ID_OutlineColor, color);
            }
        }

        private static void ApplyGlow(Material material, TextMeshProEffectController.GlowSettings settings)
        {
            SetKeyword(material, ShaderUtilities.Keyword_Glow, settings.Enabled);

            if (material.HasProperty(ShaderUtilities.ID_GlowColor))
            {
                Color color = settings.Color;
                color.a = settings.Enabled ? color.a : 0f;
                material.SetColor(ShaderUtilities.ID_GlowColor, color);
            }

            if (material.HasProperty(ShaderUtilities.ID_GlowOffset))
            {
                material.SetFloat(ShaderUtilities.ID_GlowOffset, settings.Enabled ? settings.Offset : 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_GlowPower))
            {
                material.SetFloat(ShaderUtilities.ID_GlowPower, settings.Enabled ? settings.Power : 0f);
            }
        }

        private static void ApplyShadow(Material material, TextMeshProEffectController.ShadowSettings settings)
        {
            SetKeyword(material, ShaderUtilities.Keyword_Underlay, settings.Enabled);

            if (material.HasProperty(ShaderUtilities.ID_UnderlayColor))
            {
                Color color = settings.Color;
                color.a = settings.Enabled ? color.a : 0f;
                material.SetColor(ShaderUtilities.ID_UnderlayColor, color);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayOffsetX))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, settings.Enabled ? settings.OffsetX : 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayOffsetY))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, settings.Enabled ? settings.OffsetY : 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlayDilate))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlayDilate, settings.Enabled ? settings.Dilate : 0f);
            }

            if (material.HasProperty(ShaderUtilities.ID_UnderlaySoftness))
            {
                material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, settings.Enabled ? settings.Softness : 0f);
            }
        }

        private static void SetKeyword(Material material, string keyword, bool enabled)
        {
            if (enabled)
            {
                material.EnableKeyword(keyword);
            }
            else
            {
                material.DisableKeyword(keyword);
            }
        }
    }
}
