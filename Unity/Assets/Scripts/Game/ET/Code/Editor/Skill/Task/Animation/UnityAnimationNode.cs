using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ET.Client.Editor
{
    /// <summary>
    /// 预览预制体上 UnityEngine.Animation 的技能节点。
    /// </summary>
    public class UnityAnimationNode : SkillNodeBase<UnityAnimationNodeData>
    {
        private readonly List<string> _animationComponentChoices = new List<string> { "(无)" };
        private readonly List<string> _animationChoices = new List<string> { "(无)" };

        private ObjectField _animationPrefabField;
        private PopupField<string> _animationComponentPopup;
        private PopupField<string> _animationPopup;
        private TextField _animationDurationField;
        private Toggle _isAnimationLoopingToggle;
        private VisualElement _previewSection;
        private IMGUIContainer _previewContainer;
        private Button _playPauseButton;

        private UnityAnimationPreviewRenderer _previewRenderer;
        private TimelineView _timelineView;
        private VisualElement _timelineContainer;
        private bool _timelineSectionFolded;
        private bool _editorUpdateRegistered;
        private EditorWindow _cachedEditorWindow;

        public UnityAnimationNode(Vector2 position) : base(NodeType.UnityAnimation, position)
        {
        }

        protected override string GetNodeTitle() => "Unity动画";

        protected override float GetNodeWidth() => 1160f;

        protected override void CreateContent()
        {
            CreateAnimationConfigSection();
            CreateTimelineSection();
        }

        private void CreateAnimationConfigSection()
        {
            VisualElement container = new VisualElement
            {
                style =
                {
                    backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 6,
                    paddingBottom = 6,
                    marginTop = 8
                }
            };

            VisualElement row1 = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            _animationPrefabField = new ObjectField("动画预制体")
            {
                objectType = typeof(GameObject),
                value = TypedData?.animationPrefab
            };
            _animationPrefabField.style.flexGrow = 0;
            _animationPrefabField.style.width = 340;
            _animationPrefabField.labelElement.style.minWidth = 70;
            _animationPrefabField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null)
                {
                    return;
                }

                GameObject prefab = evt.newValue as GameObject;
                if (prefab != null && !PrefabUtility.IsPartOfPrefabAsset(prefab))
                {
                    prefab = null;
                    _animationPrefabField.SetValueWithoutNotify(null);
                    Debug.LogWarning("[UnityAnimationNode] 这里只支持选择预制体资源，不支持场景对象。");
                }

                TypedData.animationPrefab = prefab;
                TypedData.animationComponentPath = string.Empty;
                TypedData.animationName = string.Empty;
                OnAnimationPrefabChanged();
                NotifyDataChanged();
            });
            row1.Add(_animationPrefabField);
            container.Add(row1);

            VisualElement row2 = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = 4 }
            };

            _animationComponentPopup = new PopupField<string>("动画节点", _animationComponentChoices, 0);
            _animationComponentPopup.style.width = 320;
            _animationComponentPopup.style.marginRight = 8;
            _animationComponentPopup.labelElement.style.minWidth = 60;
            _animationComponentPopup.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null || evt.newValue == "(无)")
                {
                    return;
                }

                TypedData.animationComponentPath = evt.newValue;
                TypedData.animationName = string.Empty;
                OnAnimationPrefabChanged();
                NotifyDataChanged();
            });
            row2.Add(_animationComponentPopup);

            _animationPopup = new PopupField<string>("动画", _animationChoices, 0);
            _animationPopup.style.width = 240;
            _animationPopup.style.marginRight = 8;
            _animationPopup.labelElement.style.minWidth = 30;
            _animationPopup.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null || evt.newValue == "(无)")
                {
                    return;
                }

                TypedData.animationName = evt.newValue;
                OnAnimationSelected(evt.newValue);
                NotifyDataChanged();
            });
            row2.Add(_animationPopup);

            _animationDurationField = new TextField("帧数") { value = TypedData?.animationDuration ?? "10" };
            _animationDurationField.style.width = 100;
            _animationDurationField.style.marginRight = 8;
            _animationDurationField.labelElement.style.minWidth = 30;
            _animationDurationField.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.animationDuration = evt.newValue;
                _timelineView?.UpdateDuration();
                NotifyDataChanged();
            });
            row2.Add(_animationDurationField);

            _isAnimationLoopingToggle = new Toggle("循环") { value = TypedData?.isAnimationLooping ?? false };
            _isAnimationLoopingToggle.style.marginRight = 8;
            _isAnimationLoopingToggle.RegisterValueChangedCallback(evt =>
            {
                if (TypedData == null)
                {
                    return;
                }

                TypedData.isAnimationLooping = evt.newValue;
                if (_previewRenderer != null && _previewRenderer.IsInitialized && !string.IsNullOrEmpty(TypedData.animationName))
                {
                    _previewRenderer.SetAnimation(TypedData.animationName, evt.newValue);
                }
                NotifyDataChanged();
            });
            row2.Add(_isAnimationLoopingToggle);

            container.Add(row2);

            _previewSection = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignItems = Align.Center
                }
            };

            _previewContainer = new IMGUIContainer(OnPreviewGUI)
            {
                style =
                {
                    width = 300,
                    height = 200,
                    backgroundColor = new Color(0.15f, 0.15f, 0.15f),
                    borderTopLeftRadius = 4,
                    borderTopRightRadius = 4,
                    borderBottomLeftRadius = 4,
                    borderBottomRightRadius = 4,
                    marginBottom = 4
                }
            };
            _previewSection.Add(_previewContainer);

            VisualElement controlRow = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.Center }
            };

            _playPauseButton = new Button(OnPlayPauseClicked) { text = "▶ 播放" };
            _playPauseButton.style.width = 80;
            _playPauseButton.style.height = 22;
            controlRow.Add(_playPauseButton);

            _previewSection.Add(controlRow);
            _previewSection.style.display = DisplayStyle.None;
            container.Add(_previewSection);
            mainContainer.Add(container);

            if (TypedData?.animationPrefab != null)
            {
                OnAnimationPrefabChanged();
            }
        }

        private void CreateTimelineSection()
        {
            _timelineContainer = new VisualElement
            {
                name = "TimelineSection",
                style =
                {
                    backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    marginTop = 8,
                    minWidth = 1144
                }
            };

            _timelineView = new TimelineView();
            _timelineView.style.display = _timelineSectionFolded ? DisplayStyle.None : DisplayStyle.Flex;
            _timelineView.OnDataChanged += NotifyDataChanged;
            _timelineView.OnAddButtonClicked += OnTimelineAddClicked;
            _timelineContainer.Add(_timelineView);

            mainContainer.Add(_timelineContainer);
            RefreshTimeline();
        }

        private void OnAnimationPrefabChanged()
        {
            CleanupPreviewRenderer();

            GameObject prefab = TypedData?.animationPrefab;
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
            {
                if (TypedData != null)
                {
                    TypedData.animationPrefab = null;
                    TypedData.animationPrefabPath = string.Empty;
                }
                _previewSection.style.display = DisplayStyle.None;
                ResetAnimationComponentChoices();
                ResetAnimationChoices();
                return;
            }

            _previewRenderer = new UnityAnimationPreviewRenderer();
            if (!_previewRenderer.Initialize(prefab, TypedData?.animationComponentPath))
            {
                CleanupPreviewRenderer();
                _previewSection.style.display = DisplayStyle.None;
                ResetAnimationComponentChoices();
                ResetAnimationChoices();
                return;
            }

            _previewSection.style.display = DisplayStyle.Flex;
            RefreshAnimationComponentChoices();
            RefreshAnimationChoices();

            RegisterEditorUpdate();
        }

        private void RefreshAnimationComponentChoices()
        {
            _animationComponentChoices.Clear();
            _animationComponentChoices.Add("(无)");

            if (_previewRenderer != null && _previewRenderer.IsInitialized)
            {
                foreach (string componentPath in _previewRenderer.AnimationComponentPaths)
                {
                    _animationComponentChoices.Add(componentPath);
                }
            }

            _animationComponentPopup.choices = _animationComponentChoices;

            string selectedPath = _previewRenderer?.SelectedAnimationComponentPath;
            if (!string.IsNullOrEmpty(selectedPath))
            {
                TypedData.animationComponentPath = selectedPath;
                _animationComponentPopup.SetValueWithoutNotify(selectedPath);
            }
            else
            {
                TypedData.animationComponentPath = string.Empty;
                _animationComponentPopup.SetValueWithoutNotify("(无)");
            }
        }

        private void RefreshAnimationChoices()
        {
            _animationChoices.Clear();
            _animationChoices.Add("(无)");

            if (_previewRenderer != null && _previewRenderer.IsInitialized)
            {
                _animationChoices.AddRange(_previewRenderer.GetAnimationNames());
            }

            _animationPopup.choices = _animationChoices;

            string currentAnimation = TypedData?.animationName ?? string.Empty;
            if (!string.IsNullOrEmpty(currentAnimation) && _animationChoices.Contains(currentAnimation))
            {
                _animationPopup.SetValueWithoutNotify(currentAnimation);
                OnAnimationSelected(currentAnimation);
            }
            else
            {
                TypedData.animationName = string.Empty;
                _animationPopup.SetValueWithoutNotify("(无)");
                _timelineView?.SetPlaybackIndicatorVisible(false);
                UpdatePlayPauseButton();
                RepaintPreview();
            }
        }

        private void OnAnimationSelected(string animationName)
        {
            if (_previewRenderer == null || !_previewRenderer.IsInitialized || string.IsNullOrEmpty(animationName) || animationName == "(无)")
            {
                return;
            }

            bool loop = TypedData?.isAnimationLooping ?? false;
            _previewRenderer.SetAnimation(animationName, loop);

            int totalFrames = _previewRenderer.TotalFrames;
            if (totalFrames > 0)
            {
                TypedData.animationDuration = totalFrames.ToString();
                _animationDurationField.SetValueWithoutNotify(totalFrames.ToString());
                _timelineView?.UpdateDuration();
            }

            _timelineView?.SetPlaybackIndicatorVisible(true);
            _timelineView?.SetPlaybackFrame(0);

            UpdatePlayPauseButton();
            RepaintPreview();
        }

        private void OnPlayPauseClicked()
        {
            if (_previewRenderer == null || !_previewRenderer.IsInitialized)
            {
                return;
            }

            _previewRenderer.TogglePlayPause();
            UpdatePlayPauseButton();
        }

        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton == null)
            {
                return;
            }

            bool playing = _previewRenderer?.IsPlaying ?? false;
            _playPauseButton.text = playing ? "⏸ 暂停" : "▶ 播放";
        }

        private void OnPreviewGUI()
        {
            if (_previewRenderer == null || !_previewRenderer.IsInitialized)
            {
                return;
            }

            Texture texture = _previewRenderer.RenderResult;
            if (texture != null)
            {
                Rect rect = GUILayoutUtility.GetRect(300, 200);
                GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            }

            if (_previewRenderer.TotalFrames > 0)
            {
                Rect infoRect = new Rect(4, 180, 292, 18);
                Color oldColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.7f);
                GUI.Label(infoRect, $"帧: {_previewRenderer.CurrentFrame} / {_previewRenderer.TotalFrames}", EditorStyles.miniLabel);
                GUI.color = oldColor;
            }
        }

        private void OnEditorUpdate()
        {
            if (_previewRenderer == null || !_previewRenderer.IsInitialized)
            {
                return;
            }

            _previewRenderer.EditorUpdate();
            if (_previewRenderer.IsPlaying && _timelineView != null)
            {
                _timelineView.SetPlaybackFrame(_previewRenderer.CurrentFrame);
            }

            UpdatePlayPauseButton();
            RepaintPreview();
        }

        private void RegisterEditorUpdate()
        {
            if (_editorUpdateRegistered)
            {
                return;
            }

            EditorApplication.update += OnEditorUpdate;
            _editorUpdateRegistered = true;
        }

        private void UnregisterEditorUpdate()
        {
            if (!_editorUpdateRegistered)
            {
                return;
            }

            EditorApplication.update -= OnEditorUpdate;
            _editorUpdateRegistered = false;
        }

        private void CleanupPreviewRenderer()
        {
            if (_previewRenderer != null)
            {
                _previewRenderer.Cleanup();
                _previewRenderer = null;
            }

            UnregisterEditorUpdate();
            _timelineView?.SetPlaybackIndicatorVisible(false);
        }

        private void ResetAnimationComponentChoices()
        {
            _animationComponentChoices.Clear();
            _animationComponentChoices.Add("(无)");
            _animationComponentPopup.choices = _animationComponentChoices;
            _animationComponentPopup?.SetValueWithoutNotify("(无)");
        }

        private void ResetAnimationChoices()
        {
            _animationChoices.Clear();
            _animationChoices.Add("(无)");
            _animationPopup.choices = _animationChoices;
            _animationPopup?.SetValueWithoutNotify("(无)");
        }

        private void OnPlaybackSeek(int frame)
        {
            if (_previewRenderer == null || !_previewRenderer.IsInitialized)
            {
                return;
            }

            _previewRenderer.SeekToFrame(frame);
            RepaintPreview();
        }

        private void RepaintPreview()
        {
            _previewContainer?.MarkDirtyRepaint();

            if (_cachedEditorWindow == null)
            {
                IPanel panel = _previewContainer?.panel;
                if (panel != null)
                {
                    foreach (EditorWindow window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                    {
                        if (window.rootVisualElement?.panel == panel)
                        {
                            _cachedEditorWindow = window;
                            break;
                        }
                    }
                }
            }

            _cachedEditorWindow?.Repaint();
        }

        private void BindPlaybackIndicator()
        {
            if (_timelineView == null)
            {
                return;
            }

            PlaybackIndicator indicator = _timelineView.GetPlaybackIndicator();
            if (indicator != null)
            {
                indicator.OnSeekToFrame -= OnPlaybackSeek;
                indicator.OnSeekToFrame += OnPlaybackSeek;
            }
        }

        private void ToggleTimelineSection()
        {
            _timelineSectionFolded = !_timelineSectionFolded;
            if (_timelineView != null)
            {
                _timelineView.style.display = _timelineSectionFolded ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        private void OnTimelineAddClicked()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("时间效果"), false, () =>
            {
                if (TypedData == null)
                {
                    return;
                }

                if (TypedData.timeEffects == null)
                {
                    TypedData.timeEffects = new List<TimeEffectData>();
                }

                TypedData.timeEffects.Add(new TimeEffectData());

                if (_timelineSectionFolded)
                {
                    ToggleTimelineSection();
                }

                _timelineView?.AddNewTrack(false);
                RefreshPorts();
                NotifyDataChanged();
            });
            menu.AddItem(new GUIContent("时间Cue"), false, () =>
            {
                if (TypedData == null)
                {
                    return;
                }

                if (TypedData.timeCues == null)
                {
                    TypedData.timeCues = new List<TimeCueData>();
                }

                TypedData.timeCues.Add(new TimeCueData());

                if (_timelineSectionFolded)
                {
                    ToggleTimelineSection();
                }

                _timelineView?.AddNewTrack(true);
                RefreshPorts();
                NotifyDataChanged();
            });
            menu.ShowAsContext();
        }

        private void RefreshTimeline()
        {
            if (_timelineView == null || TypedData == null)
            {
                return;
            }

            _timelineView.Initialize(TypedData, () =>
            {
                Port port = TimelinePort.Create(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
                return port;
            });

            RefreshPorts();
            BindPlaybackIndicator();
        }

        public override Port FindOutputPortByIdentifier(int portId)
        {
            if (_timelineView != null)
            {
                Port port = _timelineView.FindPortByIdentifier(portId);
                if (port != null)
                {
                    return port;
                }
            }

            return base.FindOutputPortByIdentifier(portId);
        }

        public override void LoadData(NodeData data)
        {
            base.LoadData(data);
            SyncUIFromData();
        }

        public override void SyncUIFromData()
        {
            base.SyncUIFromData();
            if (TypedData == null)
            {
                return;
            }

            _animationPrefabField?.SetValueWithoutNotify(TypedData.animationPrefab);
            _animationDurationField?.SetValueWithoutNotify(TypedData.animationDuration ?? "10");
            _isAnimationLoopingToggle?.SetValueWithoutNotify(TypedData.isAnimationLooping);

            if (TypedData.animationPrefab != null)
            {
                OnAnimationPrefabChanged();
            }
            else
            {
                ResetAnimationComponentChoices();
                ResetAnimationChoices();
            }

            RefreshTimeline();
        }

        ~UnityAnimationNode()
        {
            CleanupPreviewRenderer();
        }
    }
}
