using System;
using System.Reflection;
using System.Collections;
using UnityEditor;
using Unity.Mathematics;

namespace ET.Client.Editor
{
    public static class FightInputRuntimeDebugMenu
    {
        private const string PublishScreenCenterClickMenuPath = "GenAtom/Runtime/Publish FightInput Screen Center Click";
        private const string DumpFightInputHandlersMenuPath = "GenAtom/Runtime/Dump FightInput Handlers";
        private const string ReloadCurrentSceneMenuPath = "GenAtom/Runtime/Reload Current Scene";

        [MenuItem(PublishScreenCenterClickMenuPath)]
        public static void PublishScreenCenterClick()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Play Mode required.");
                return;
            }

            Scene currentScene = GetCurrentClientScene();
            if (currentScene == null)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Current scene not found.");
                return;
            }

            UnityEngine.Camera camera = UnityEngine.Camera.main;
            if (camera == null)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Current scene camera not found.");
                return;
            }

            float2 screenPosition = new float2(camera.pixelWidth * 0.5f, camera.pixelHeight * 0.5f);
            EventSystem.Instance.Publish(currentScene, new FightInputScreenClick
            {
                ScreenPosition = screenPosition,
            });

            UnityEngine.Debug.LogWarning(
                $"[FightInputDebug] Publish screen center click scene={currentScene.SceneType} screen=({screenPosition.x:0.##},{screenPosition.y:0.##})");
        }

        [MenuItem(DumpFightInputHandlersMenuPath)]
        public static void DumpFightInputHandlers()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Play Mode required.");
                return;
            }

            EventSystem eventSystem = EventSystem.Instance;
            if (eventSystem == null)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] EventSystem is null.");
                return;
            }

            FieldInfo allEventsField = typeof(EventSystem).GetField("allEvents", BindingFlags.Instance | BindingFlags.NonPublic);
            if (allEventsField == null)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] allEvents field not found.");
                return;
            }

            IDictionary allEvents = allEventsField.GetValue(eventSystem) as IDictionary;
            if (allEvents == null)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] allEvents dictionary is null.");
                return;
            }

            IList eventInfos = allEvents[typeof(FightInputScreenClick)] as IList;
            if (eventInfos == null || eventInfos.Count == 0)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] No handlers registered for FightInputScreenClick.");
                return;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.Append("[FightInputDebug] Registered handlers:");
            foreach (object eventInfo in eventInfos)
            {
                if (eventInfo == null)
                {
                    continue;
                }

                System.Type eventInfoType = eventInfo.GetType();
                FieldInfo iEventField = eventInfoType.GetField("<IEvent>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo sceneTypeField = eventInfoType.GetField("<SceneType>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                object iEvent = iEventField?.GetValue(eventInfo);
                object sceneType = sceneTypeField?.GetValue(eventInfo);
                builder.Append($" [{iEvent?.GetType().FullName ?? "null"}|sceneType={sceneType}]");
            }

            UnityEngine.Debug.LogWarning(builder.ToString());
        }

        [MenuItem(ReloadCurrentSceneMenuPath)]
        public static void ReloadCurrentScene()
        {
            if (!EditorApplication.isPlaying)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Play Mode required.");
                return;
            }

            Scene currentScene = GetCurrentClientScene();
            if (currentScene == null || currentScene.IsDisposed)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Current scene not found.");
                return;
            }

            Scene root = currentScene.Root();
            if (root == null || root.IsDisposed)
            {
                UnityEngine.Debug.LogWarning("[FightInputDebug] Root scene not found.");
                return;
            }

            UnityEngine.Debug.LogWarning($"[FightInputDebug] reload begin sceneName={currentScene.Name} sceneId={currentScene.Id}");
            MethodInfo reloadMethod = typeof(SceneChangeHelper).GetMethod(nameof(SceneChangeHelper.SceneChangeTo2), BindingFlags.Public | BindingFlags.Static);
            reloadMethod?.Invoke(null, new object[] { root, currentScene.Name, currentScene.Id });
        }

        private static Scene GetCurrentClientScene()
        {
            FiberManager fiberManager = FiberManager.Instance;
            if (fiberManager == null)
            {
                return null;
            }

            MethodInfo getMethod = typeof(FiberManager).GetMethod("Get", BindingFlags.Instance | BindingFlags.NonPublic);
            if (getMethod == null)
            {
                return null;
            }

            Fiber mainFiber = getMethod.Invoke(fiberManager, new object[] { ConstFiberId.Main }) as Fiber;
            Scene root = mainFiber?.Root;
            return root?.CurrentScene();
        }
    }
}
