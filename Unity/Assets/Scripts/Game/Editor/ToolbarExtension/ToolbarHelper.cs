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
        private const string ToolbarContainerName = "GameDevelopmentKitToolbar";
        private const string LegacyLeftContainerName = "GameDevelopmentKitToolbar_Left";
        private const string LegacyRightContainerName = "GameDevelopmentKitToolbar_Right";
        private const string PackageLeftContainerName = "ToolbarExtension_Left";
        private const string PackageRightContainerName = "ToolbarExtension_Right";
        private const float ToolbarLeft = 520f;
        private const float ToolbarTop = 5f;

        private static readonly List<ToolbarElementMethod> s_LeftElements = new List<ToolbarElementMethod>();
        private static readonly List<ToolbarElementMethod> s_RightElements = new List<ToolbarElementMethod>();

        private static EditorWindow s_MainToolbarWindow;
        private static VisualElement s_ToolbarRootVisualElement;
        private static bool s_Registered;

        static ToolbarHelper()
        {
            CollectToolbarElements();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
        }

        private static void CollectToolbarElements()
        {
            s_LeftElements.Clear();
            s_RightElements.Clear();

            CollectToolbarButtons();
            CollectToolbarDropdowns();
            CollectLegacyToolbars();

            s_LeftElements.Sort((left, right) => left.Priority - right.Priority);
            s_RightElements.Sort((left, right) => right.Priority - left.Priority);
        }

        private static void CollectToolbarButtons()
        {
            Type attributeType = typeof(ToolbarButtonAttribute);

            foreach (MethodInfo methodInfo in TypeCache.GetMethodsWithAttribute<ToolbarButtonAttribute>())
            {
                object[] attributes = methodInfo.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0 || !IsValidStaticMethod(methodInfo, 0))
                {
                    continue;
                }

                ToolbarButtonAttribute attribute = (ToolbarButtonAttribute)attributes[0];
                ToolbarElementMethod toolbarButton = ToolbarElementMethod.CreateButton(attribute.Priority, attribute.Text, attribute.Tooltip, methodInfo);
                AddToolbarElement(attribute.Side, toolbarButton);
            }
        }

        private static void CollectToolbarDropdowns()
        {
            Type attributeType = typeof(ToolbarDropdownAttribute);

            foreach (MethodInfo methodInfo in TypeCache.GetMethodsWithAttribute<ToolbarDropdownAttribute>())
            {
                object[] attributes = methodInfo.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0 || !IsValidStaticMethod(methodInfo, 1))
                {
                    continue;
                }

                ParameterInfo[] parameters = methodInfo.GetParameters();
                if (parameters[0].ParameterType != typeof(GenericMenu))
                {
                    continue;
                }

                ToolbarDropdownAttribute attribute = (ToolbarDropdownAttribute)attributes[0];
                ToolbarElementMethod toolbarDropdown = ToolbarElementMethod.CreateDropdown(attribute.Priority, attribute.Text, attribute.Tooltip, methodInfo);
                AddToolbarElement(attribute.Side, toolbarDropdown);
            }
        }

        private static void CollectLegacyToolbars()
        {
            Type attributeType = typeof(ToolbarAttribute);

            foreach (MethodInfo methodInfo in TypeCache.GetMethodsWithAttribute<ToolbarAttribute>())
            {
                object[] attributes = methodInfo.GetCustomAttributes(attributeType, false);
                if (attributes.Length == 0 || !IsValidStaticMethod(methodInfo, 0))
                {
                    continue;
                }

                ToolbarAttribute attribute = (ToolbarAttribute)attributes[0];
                string text = ObjectNames.NicifyVariableName(methodInfo.Name);
                ToolbarElementMethod legacyToolbar = ToolbarElementMethod.CreateLegacyGUI(attribute.Priority, text, methodInfo);
                AddToolbarElement(attribute.Side, legacyToolbar);
            }
        }

        private static bool IsValidStaticMethod(MethodInfo methodInfo, int parameterCount)
        {
            return methodInfo.IsStatic && methodInfo.GetParameters().Length == parameterCount;
        }

        private static void AddToolbarElement(OnGUISide side, ToolbarElementMethod toolbarElement)
        {
            if (side == OnGUISide.Left)
            {
                s_LeftElements.Add(toolbarElement);
                return;
            }

            if (side == OnGUISide.Right)
            {
                s_RightElements.Add(toolbarElement);
            }
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

            if (s_Registered && s_ToolbarRootVisualElement.Q(ToolbarContainerName) != null)
            {
                return;
            }

            s_Registered = RegisterElements();
        }

        private static bool TryGetToolbarRoot(out VisualElement root)
        {
            root = null;

            if (s_MainToolbarWindow == null)
            {
                foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    if (window.GetType().FullName == "UnityEditor.MainToolbarWindow")
                    {
                        s_MainToolbarWindow = window;
                        break;
                    }
                }

                if (s_MainToolbarWindow == null)
                {
                    return false;
                }
            }

            root = s_MainToolbarWindow.rootVisualElement;
            return root != null;
        }

        private static bool RegisterElements()
        {
            if (s_ToolbarRootVisualElement == null)
            {
                return false;
            }

            RemoveOldContainers();

            VisualElement container = new VisualElement
            {
                name = ToolbarContainerName,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexShrink = 0,
                    flexGrow = 0,
                    position = Position.Absolute,
                    left = ToolbarLeft,
                    top = ToolbarTop,
                }
            };

            bool toolsInserted = false;
            for (int i = 0; i < s_LeftElements.Count; i++)
            {
                AddElement(container, s_LeftElements[i]);

                if (!toolsInserted && s_RightElements.Count > 0)
                {
                    AddToolsMenu(container);
                    toolsInserted = true;
                }
            }

            if (!toolsInserted && s_RightElements.Count > 0)
            {
                AddToolsMenu(container);
            }

            s_ToolbarRootVisualElement.Add(container);
            return true;
        }

        private static void RemoveOldContainers()
        {
            RemoveContainer(ToolbarContainerName);
            RemoveContainer(LegacyLeftContainerName);
            RemoveContainer(LegacyRightContainerName);
            RemoveContainer(PackageLeftContainerName);
            RemoveContainer(PackageRightContainerName);
        }

        private static void RemoveContainer(string containerName)
        {
            VisualElement container = s_ToolbarRootVisualElement.Q(containerName);
            container?.RemoveFromHierarchy();
        }

        private static void AddElement(VisualElement container, ToolbarElementMethod elementMethod)
        {
            VisualElement element = elementMethod.CreateElement();
            element.style.height = 20;
            element.style.marginLeft = 1;
            element.style.marginRight = 1;
            element.style.flexShrink = 0;
            container.Add(element);
        }

        private static void AddToolsMenu(VisualElement container)
        {
            Button toolsButton = null;
            toolsButton = new Button(() => ShowToolsMenu(toolsButton))
            {
                text = "Tools v",
                tooltip = "GameDevelopmentKit toolbar tools.",
            };
            toolsButton.style.height = 20;
            toolsButton.style.marginLeft = 1;
            toolsButton.style.marginRight = 4;
            toolsButton.style.paddingLeft = 5;
            toolsButton.style.paddingRight = 5;
            toolsButton.style.flexShrink = 0;
            container.Add(toolsButton);
        }

        private static void ShowToolsMenu(VisualElement anchor)
        {
            GenericMenu menu = new GenericMenu();
            foreach (ToolbarElementMethod element in s_RightElements)
            {
                element.AppendToMenu(menu);
            }

            ShowGenericMenu(anchor, menu);
        }

        private static void ShowGenericMenu(VisualElement anchor, GenericMenu menu)
        {
            Rect worldBound = anchor.worldBound;
            menu.DropDown(new Rect(worldBound.x, worldBound.yMax, worldBound.width, worldBound.height));
        }

        private enum ToolbarElementKind
        {
            Button,
            Dropdown,
            LegacyGUI,
        }

        private sealed class ToolbarElementMethod
        {
            public readonly int Priority;
            private readonly string m_Text;
            private readonly string m_Tooltip;
            private readonly MethodInfo m_MethodInfo;
            private readonly ToolbarElementKind m_Kind;

            private ToolbarElementMethod(int priority, string text, string tooltip, MethodInfo methodInfo, ToolbarElementKind kind)
            {
                Priority = priority;
                m_Text = text;
                m_Tooltip = tooltip;
                m_MethodInfo = methodInfo;
                m_Kind = kind;
            }

            public static ToolbarElementMethod CreateButton(int priority, string text, string tooltip, MethodInfo methodInfo)
            {
                return new ToolbarElementMethod(priority, text, tooltip, methodInfo, ToolbarElementKind.Button);
            }

            public static ToolbarElementMethod CreateDropdown(int priority, string text, string tooltip, MethodInfo methodInfo)
            {
                return new ToolbarElementMethod(priority, text, tooltip, methodInfo, ToolbarElementKind.Dropdown);
            }

            public static ToolbarElementMethod CreateLegacyGUI(int priority, string text, MethodInfo methodInfo)
            {
                return new ToolbarElementMethod(priority, text, text, methodInfo, ToolbarElementKind.LegacyGUI);
            }

            public VisualElement CreateElement()
            {
                if (m_Kind == ToolbarElementKind.Dropdown)
                {
                    Button dropdownButton = null;
                    dropdownButton = new Button(() => ShowDropdownMenu(dropdownButton))
                    {
                        text = m_Text + " v",
                        tooltip = m_Tooltip,
                    };
                    dropdownButton.style.paddingLeft = 5;
                    dropdownButton.style.paddingRight = 5;
                    return dropdownButton;
                }

                if (m_Kind == ToolbarElementKind.LegacyGUI)
                {
                    IMGUIContainer container = new IMGUIContainer(Invoke);
                    container.tooltip = m_Tooltip;
                    return container;
                }

                Button button = new Button(Invoke)
                {
                    text = m_Text,
                    tooltip = m_Tooltip,
                };
                button.style.paddingLeft = 5;
                button.style.paddingRight = 5;
                return button;
            }

            public void AppendToMenu(GenericMenu menu)
            {
                if (m_Kind == ToolbarElementKind.Dropdown)
                {
                    m_MethodInfo.Invoke(null, new object[] { menu });
                    return;
                }

                if (m_Kind == ToolbarElementKind.LegacyGUI)
                {
                    menu.AddDisabledItem(new GUIContent(m_Text));
                    return;
                }

                menu.AddItem(new GUIContent(m_Text), false, Invoke);
            }

            private void Invoke()
            {
                m_MethodInfo.Invoke(null, null);
            }

            private void ShowDropdownMenu(VisualElement anchor)
            {
                GenericMenu menu = new GenericMenu();
                m_MethodInfo.Invoke(null, new object[] { menu });
                ShowGenericMenu(anchor, menu);
            }
        }
    }
}
