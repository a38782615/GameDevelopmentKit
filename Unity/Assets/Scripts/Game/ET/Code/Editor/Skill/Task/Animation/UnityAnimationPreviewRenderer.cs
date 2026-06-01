using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ET.Client.Editor
{
    /// <summary>
    /// 在编辑器中预览挂有 UnityEngine.Animation 的预制体。
    /// </summary>
    public sealed class UnityAnimationPreviewRenderer : IDisposable
    {
        private const int PreviewLayer = 30;
        private const int PreviewCameraCullingMask = 1 << PreviewLayer;

        private readonly List<string> _animationNames = new List<string>();
        private readonly List<string> _animationComponentPaths = new List<string>();

        private GameObject _cameraObject;
        private Camera _camera;
        private GameObject _previewObject;
        private UnityEngine.Animation _animationComponent;
        private GameObject _sampleTargetObject;
        private RenderTexture _renderTexture;

        private bool _isInitialized;
        private bool _isPlaying;
        private bool _isLooping;
        private int _currentFrame;
        private int _totalFrames;
        private float _animationDuration;
        private double _lastEditorTime;
        private int _width;
        private int _height;
        private string _selectedAnimationName = string.Empty;
        private string _selectedAnimationComponentPath = string.Empty;

        public int CurrentFrame => _currentFrame;
        public int TotalFrames => _totalFrames;
        public bool IsPlaying => _isPlaying;
        public Texture RenderResult => _renderTexture;
        public bool IsInitialized => _isInitialized;
        public IReadOnlyList<string> AnimationComponentPaths => _animationComponentPaths;
        public string SelectedAnimationComponentPath => _selectedAnimationComponentPath;

        public bool Initialize(GameObject prefab, string animationComponentPath, int width = 300, int height = 200)
        {
            Cleanup();

            if (prefab == null)
            {
                return false;
            }

            _width = width;
            _height = height;

            try
            {
                _previewObject = UnityEngine.Object.Instantiate(prefab);
                if (_previewObject == null)
                {
                    Cleanup();
                    return false;
                }

                ApplyHideFlags(_previewObject);
                ApplyPreviewLayer(_previewObject.transform);
                _previewObject.SetActive(true);

                UnityEngine.Animation[] animationComponents = _previewObject.GetComponentsInChildren<UnityEngine.Animation>(true);
                if (animationComponents == null || animationComponents.Length == 0)
                {
                    Cleanup();
                    return false;
                }

                _animationComponentPaths.Clear();
                foreach (UnityEngine.Animation animation in animationComponents)
                {
                    _animationComponentPaths.Add(GetTransformPath(animation.transform, _previewObject.transform));
                }

                _animationComponent = FindAnimationComponent(animationComponents, animationComponentPath);
                if (_animationComponent == null)
                {
                    Cleanup();
                    return false;
                }

                _sampleTargetObject = _animationComponent.gameObject;
                _selectedAnimationComponentPath = GetTransformPath(_animationComponent.transform, _previewObject.transform);

                _cameraObject = new GameObject("UnityAnimationPreviewCamera");
                _cameraObject.hideFlags = HideFlags.HideAndDontSave;

                _camera = _cameraObject.AddComponent<Camera>();
                _camera.orthographic = true;
                _camera.cullingMask = PreviewCameraCullingMask;
                _camera.nearClipPlane = 0.01f;
                _camera.farClipPlane = 1000f;
                _camera.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.enabled = false;

                _renderTexture = new RenderTexture(_width, _height, 16, RenderTextureFormat.ARGB32);
                _renderTexture.Create();

                CollectAnimationNames();
                SampleCurrentAnimation(0f);
                RenderFrame();

                _isInitialized = true;
                _lastEditorTime = EditorApplication.timeSinceStartup;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[UnityAnimationPreviewRenderer] Initialize failed: {exception.Message}");
                Cleanup();
                return false;
            }
        }

        public List<string> GetAnimationNames()
        {
            return _animationNames;
        }

        public void SetAnimation(string animationName, bool loop)
        {
            if (!_isInitialized || _animationComponent == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            AnimationState state = _animationComponent[animationName];
            if (state == null || state.clip == null)
            {
                return;
            }

            _selectedAnimationName = animationName;
            _isLooping = loop;
            _animationDuration = state.length;
            _totalFrames = SkillEditorConstants.SecondsToFrames(_animationDuration);
            if (_totalFrames <= 0)
            {
                _totalFrames = 1;
            }

            _currentFrame = 0;
            _isPlaying = false;
            SeekToFrame(0);
        }

        public void SeekToFrame(int frame)
        {
            if (!_isInitialized || string.IsNullOrEmpty(_selectedAnimationName))
            {
                return;
            }

            _currentFrame = Mathf.Clamp(frame, 0, _totalFrames);
            float targetTime = SkillEditorConstants.FramesToSeconds(_currentFrame);
            SampleCurrentAnimation(targetTime);
            RenderFrame();
        }

        public void TogglePlayPause()
        {
            if (!_isInitialized || string.IsNullOrEmpty(_selectedAnimationName))
            {
                return;
            }

            _isPlaying = !_isPlaying;
            _lastEditorTime = EditorApplication.timeSinceStartup;
        }

        public void EditorUpdate()
        {
            if (!_isInitialized || !_isPlaying || string.IsNullOrEmpty(_selectedAnimationName))
            {
                return;
            }

            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = Mathf.Min((float)(currentTime - _lastEditorTime), 0.1f);
            _lastEditorTime = currentTime;

            float currentTimeSeconds = SkillEditorConstants.FramesToSeconds(_currentFrame) + deltaTime;
            if (_animationDuration <= 0f)
            {
                _currentFrame = 0;
                _isPlaying = false;
                return;
            }

            if (_isLooping)
            {
                currentTimeSeconds %= _animationDuration;
            }
            else if (currentTimeSeconds >= _animationDuration)
            {
                currentTimeSeconds = _animationDuration;
                _isPlaying = false;
            }

            _currentFrame = SkillEditorConstants.SecondsToFrames(currentTimeSeconds);
            if (!_isLooping && currentTimeSeconds >= _animationDuration)
            {
                _currentFrame = _totalFrames;
            }
            else
            {
                _currentFrame = Mathf.Clamp(_currentFrame, 0, _totalFrames);
            }

            SampleCurrentAnimation(currentTimeSeconds);
            RenderFrame();
        }

        public void Cleanup()
        {
            _isInitialized = false;
            _isPlaying = false;
            _isLooping = false;
            _currentFrame = 0;
            _totalFrames = 0;
            _animationDuration = 0f;
            _selectedAnimationName = string.Empty;
            _selectedAnimationComponentPath = string.Empty;
            _animationNames.Clear();
            _animationComponentPaths.Clear();
            _animationComponent = null;
            _sampleTargetObject = null;
            _camera = null;

            if (_previewObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewObject);
                _previewObject = null;
            }

            if (_cameraObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_cameraObject);
                _cameraObject = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                UnityEngine.Object.DestroyImmediate(_renderTexture);
                _renderTexture = null;
            }
        }

        public void Dispose()
        {
            Cleanup();
        }

        private void CollectAnimationNames()
        {
            _animationNames.Clear();
            if (_animationComponent == null)
            {
                return;
            }

            foreach (AnimationState state in _animationComponent)
            {
                if (state?.clip == null || string.IsNullOrEmpty(state.name))
                {
                    continue;
                }

                if (!_animationNames.Contains(state.name))
                {
                    _animationNames.Add(state.name);
                }
            }
        }

        private void SampleCurrentAnimation(float timeSeconds)
        {
            if (_animationComponent == null || _sampleTargetObject == null || string.IsNullOrEmpty(_selectedAnimationName))
            {
                AdjustCamera();
                return;
            }

            AnimationState state = _animationComponent[_selectedAnimationName];
            if (state?.clip == null)
            {
                AdjustCamera();
                return;
            }

            float sampleTime = Mathf.Clamp(timeSeconds, 0f, Mathf.Max(state.length, 0f));
            state.clip.SampleAnimation(_sampleTargetObject, sampleTime);
            AdjustCamera();
        }

        private void RenderFrame()
        {
            if (_camera == null || _renderTexture == null)
            {
                return;
            }

            RenderTexture previous = RenderTexture.active;
            _camera.targetTexture = _renderTexture;
            _camera.Render();
            _camera.targetTexture = null;
            RenderTexture.active = previous;
        }

        private void AdjustCamera()
        {
            if (_camera == null || _previewObject == null)
            {
                return;
            }

            Renderer[] renderers = _previewObject.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                _camera.orthographicSize = 1f;
                _camera.transform.position = new Vector3(0f, 0f, -10f);
                _camera.transform.rotation = Quaternion.identity;
                return;
            }

            bool hasBounds = false;
            Bounds bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasBounds)
            {
                _camera.orthographicSize = 1f;
                _camera.transform.position = new Vector3(0f, 0f, -10f);
                _camera.transform.rotation = Quaternion.identity;
                return;
            }

            _camera.orthographicSize = Mathf.Max(bounds.size.y, bounds.size.x * _height / (float)_width) * 0.55f;
            if (_camera.orthographicSize < 0.1f)
            {
                _camera.orthographicSize = 0.1f;
            }

            _camera.transform.position = bounds.center + new Vector3(0f, 0f, -10f);
            _camera.transform.rotation = Quaternion.identity;
        }

        private static UnityEngine.Animation FindAnimationComponent(UnityEngine.Animation[] animationComponents, string targetPath)
        {
            if (animationComponents == null || animationComponents.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(targetPath))
            {
                foreach (UnityEngine.Animation animation in animationComponents)
                {
                    if (animation == null)
                    {
                        continue;
                    }

                    string path = GetTransformPath(animation.transform, animationComponents[0].transform.root);
                    if (string.Equals(path, targetPath, StringComparison.Ordinal))
                    {
                        return animation;
                    }
                }
            }

            return animationComponents[0];
        }

        private static string GetTransformPath(Transform current, Transform root)
        {
            if (current == null)
            {
                return string.Empty;
            }

            if (current == root)
            {
                return current.name;
            }

            Stack<string> names = new Stack<string>();
            Transform cursor = current;
            while (cursor != null)
            {
                names.Push(cursor.name);
                if (cursor == root)
                {
                    break;
                }

                cursor = cursor.parent;
            }

            return string.Join("/", names.ToArray());
        }

        private static void ApplyHideFlags(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        private static void ApplyPreviewLayer(Transform root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                transform.gameObject.layer = PreviewLayer;
            }
        }
    }
}
