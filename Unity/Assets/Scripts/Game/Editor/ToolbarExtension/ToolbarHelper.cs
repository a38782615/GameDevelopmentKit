using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ToolbarExtension
{
    [InitializeOnLoad]
    internal static class ToolbarHelper
    {
        private const string LeftZoneName = "ToolbarZoneLeftAlign";
        private const string RightZoneName = "ToolbarZoneRightAlign";
        private const string LeftContainerName = "ToolbarExtension_Left";
        private const string RightContainerName = "ToolbarExtension_Right";

        private static readonly Type s_ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static readonly List<(int Priority, Action Handler)> s_LeftToolbarGUI = new List<(int Priority, Action Handler)>();
        private static readonly List<(int Priority, Action Handler)> s_RightToolbarGUI = new List<(int Priority, Action Handler)>();

        private static ScriptableObject s_ToolbarScriptableObject;
        private static FieldInfo s_ToolbarRootFieldInfo;
        private static VisualElement s_ToolbarRootVisualElement;
        private static bool s_Registered;

        static ToolbarHelper()
        {
            CollectToolbarMethods();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void CollectToolbarMethods()
        {
            Type attributeType = typeof(ToolbarAttribute);

            foreach (MethodInfo methodInfo in TypeCache.GetMethodsWithAttribute<ToolbarAttribute>())
            {
                object[] attributes = methodInfo.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0 || !methodInfo.IsStatic)
                {
                    continue;
                }

                ToolbarAttribute attribute = (ToolbarAttribute)attributes[0];
                Action handler = () => methodInfo.Invoke(null, null);

                if (attribute.Side == OnGUISide.Left)
                {
                    s_LeftToolbarGUI.Add((attribute.Priority, handler));
                    continue;
                }

                if (attribute.Side == OnGUISide.Right)
                {
                    s_RightToolbarGUI.Add((attribute.Priority, handler));
                }
            }

            s_LeftToolbarGUI.Sort((left, right) => left.Priority - right.Priority);
            s_RightToolbarGUI.Sort((left, right) => right.Priority - left.Priority);
        }

        private static void OnUpdate()
        {
            if (!TryGetToolbarRoot(out VisualElement root))
            {
                return;
            }

            if (s_ToolbarRootVisualElement != root)
            {
                s_ToolbarRootVisualElement = root;
                s_Registered = false;
            }

            if (s_Registered)
            {
                return;
            }

            bool leftRegistered = RegisterCallback(LeftZoneName, LeftContainerName, GUILeft);
            bool rightRegistered = RegisterCallback(RightZoneName, RightContainerName, GUIRight);
            s_Registered = leftRegistered && rightRegistered;
        }

        private static bool TryGetToolbarRoot(out VisualElement root)
        {
            root = null;

            if (s_ToolbarType == null)
            {
                return false;
            }

            if (s_ToolbarScriptableObject == null)
            {
                UnityEngine.Object[] toolbars = Resources.FindObjectsOfTypeAll(s_ToolbarType);
                s_ToolbarScriptableObject = toolbars.Length > 0 ? toolbars[0] as ScriptableObject : null;
            }

            if (s_ToolbarScriptableObject == null)
            {
                return false;
            }

            if (s_ToolbarRootFieldInfo == null)
            {
                s_ToolbarRootFieldInfo = s_ToolbarScriptableObject.GetType()
                    .GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (s_ToolbarRootFieldInfo == null)
            {
                return false;
            }

            root = s_ToolbarRootFieldInfo.GetValue(s_ToolbarScriptableObject) as VisualElement;
            return root != null;
        }

        private static bool RegisterCallback(string zoneName, string containerName, Action callback)
        {
            VisualElement toolbarZone = s_ToolbarRootVisualElement?.Q(zoneName);
            if (toolbarZone == null)
            {
                return false;
            }

            if (toolbarZone.Q(containerName) != null)
            {
                return true;
            }

            VisualElement parent = new VisualElement
            {
                name = containerName,
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Row,
                }
            };

            IMGUIContainer container = new IMGUIContainer(() => callback?.Invoke());
            container.style.flexGrow = 1;
            parent.Add(container);
            toolbarZone.Add(parent);
            return true;
        }

        private static void GUILeft()
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            foreach ((int Priority, Action Handler) handler in s_LeftToolbarGUI)
            {
                handler.Handler();
            }

            GUILayout.EndHorizontal();
        }

        private static void GUIRight()
        {
            GUILayout.BeginHorizontal();
            foreach ((int Priority, Action Handler) handler in s_RightToolbarGUI)
            {
                handler.Handler();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
