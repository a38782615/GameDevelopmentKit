using System;
using System.Reflection;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Editor
{
    internal static class UrpBlitterLifecycleFix
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= CleanupBlitter;
            AssemblyReloadEvents.beforeAssemblyReload += CleanupBlitter;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            EditorApplication.delayCall -= CleanupBlitterIfPipelineMissing;
            EditorApplication.delayCall += CleanupBlitterIfPipelineMissing;
        }

        [MenuItem("Game/Tool/Cleanup URP Blitter")]
        private static void CleanupBlitterByMenu()
        {
            CleanupBlitter();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.ExitingPlayMode)
            {
                CleanupBlitter();
            }
        }

        private static void CleanupBlitterIfPipelineMissing()
        {
            if (RenderPipelineManager.currentPipeline == null)
            {
                CleanupBlitter();
            }
        }

        private static void CleanupBlitter()
        {
            try
            {
                Type blitterType = Type.GetType("UnityEngine.Rendering.Blitter, Unity.RenderPipelines.Core.Runtime");
                MethodInfo cleanupMethod = blitterType?.GetMethod("Cleanup", BindingFlags.Public | BindingFlags.Static);
                cleanupMethod?.Invoke(null, null);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(Utility.Text.Format("Cleanup URP Blitter failed: {0}", exception.Message));
            }
        }
    }
}
