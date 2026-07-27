using System.Reflection;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ET.Client.Editor
{
    public static class UIFormSkillsRuntimeDebugMenu
    {
        private const string MenuPath = "ET/GenAtom/Runtime/Open UIFormSkills";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[UIFormSkillsRuntimeDebug] Play Mode required.");
                return;
            }

            Scene currentScene = GetCurrentClientScene();
            Scene root = currentScene?.Root();
            if (currentScene == null || root == null)
            {
                Debug.LogWarning("[UIFormSkillsRuntimeDebug] Current client scene not found.");
                return;
            }

            EventSystem.Instance.PublishAsync(root, new GoScene
            {
                SceneId = (int)currentScene.Id,
                UI = UGFUIFormId.UIFormSkills,
            }).Forget();
        }

        private static Scene GetCurrentClientScene()
        {
            FiberManager fiberManager = FiberManager.Instance;
            MethodInfo getMethod = typeof(FiberManager).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);
            Fiber mainFiber = getMethod?.Invoke(fiberManager, new object[] { ConstFiberId.Main }) as Fiber;
            return mainFiber?.Root?.CurrentScene();
        }
    }
}
