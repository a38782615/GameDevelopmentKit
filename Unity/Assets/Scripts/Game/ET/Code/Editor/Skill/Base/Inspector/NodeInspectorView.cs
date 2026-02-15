using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using System.Reflection;


namespace ET.Editor
{
    public class NodeInspectorView : VisualElement
    {
        private SkillNodeBase currentNode;
        private NodeInspectorBase currentInspector;
        private VisualElement contentContainer;
        private ScrollView scrollView;
        private SkillGraphView graphView;
        private SkillGraphData graphData;
        private string filePath;
        private Label titleLabel;
        private bool isShowingSkillAsset;

        public event System.Action OnNodeDataModified;

        public NodeInspectorView()
        {
            style.width = 300;
            style.backgroundColor = new Color(56f / 255f, 56f / 255f, 56f / 255f);
            style.paddingTop = 10;
            style.paddingBottom = 10;
            style.paddingLeft = 10;
            style.paddingRight = 10;
            style.flexDirection = FlexDirection.Column;

            titleLabel = new Label("属性")
            {
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10
                }
            };
            Add(titleLabel);

            // 创建滚动视图
            scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };

            contentContainer = new VisualElement
            {
                style =
                {
                    flexGrow = 1
                }
            };
            scrollView.Add(contentContainer);
            Add(scrollView);
        }

        public void SetGraphContext(SkillGraphView view, SkillGraphData data, string path)
        {
            graphView = view;
            graphData = data;
            filePath = path;

            if (currentInspector != null)
            {
                currentInspector.SetContext(view, data, path);
            }

            // 设置上下文后，如果有数据则显示技能资源信息
            if (data != null)
            {
                ShowSkillAssetInfo();
            }
        }

        /// <summary>
        /// 显示技能资源（SkillGraphData）的属性信息
        /// </summary>
        public void ShowSkillAssetInfo()
        {
            if (graphData == null)
            {
                contentContainer.Clear();
                var noDataLabel = new Label("未选择技能")
                {
                    style =
                    {
                        fontSize = 14,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        marginTop = 20
                    }
                };
                contentContainer.Add(noDataLabel);
                return;
            }

            // 取消订阅旧节点的事件
            if (currentNode != null)
            {
                currentNode.OnDataChanged -= OnNodeDataChanged;
                currentNode = null;
            }

            isShowingSkillAsset = true;
            titleLabel.text = "技能属性";
            contentContainer.Clear();

            // 显示文件名
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var fileNameLabel = new Label($"文件: {fileName}")
            {
                style = { marginBottom = 5 }
            };
            contentContainer.Add(fileNameLabel);

            var separator = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f),
                    marginTop = 5,
                    marginBottom = 10
                }
            };
            contentContainer.Add(separator);

            // 使用 SerializedObject 绑定 SkillGraphData 的属性
            var serializedObject = new SerializedObject(graphData);

            // SkillId 字段
            var skillIdProperty = serializedObject.FindProperty("SkillId");
            if (skillIdProperty != null)
            {
                var skillIdField = new PropertyField(skillIdProperty, "技能ID");
                skillIdField.Bind(serializedObject);
                skillIdField.RegisterValueChangeCallback(evt =>
                {
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(graphData);
                });
                contentContainer.Add(skillIdField);
            }

            // 显示节点数量信息（只读）
            var nodeCountLabel = new Label($"节点数量: {graphData.nodes?.Count ?? 0}")
            {
                style = { marginTop = 10 }
            };
            contentContainer.Add(nodeCountLabel);

            var connectionCountLabel = new Label($"连接数量: {graphData.connections?.Count ?? 0}");
            contentContainer.Add(connectionCountLabel);

            // 定位资源按钮
            var locateButton = new Button(() =>
            {
                if (graphData != null)
                {
                    EditorGUIUtility.PingObject(graphData);
                    Selection.activeObject = graphData;
                }
            })
            {
                text = "定位资源",
                style =
                {
                    marginTop = 15,
                    height = 24
                }
            };
            contentContainer.Add(locateButton);
        }

        public void UpdateSelection(SkillNodeBase node)
        {
            // 如果传入 null，恢复显示技能资源信息
            if (node == null)
            {
                if (currentNode != null)
                {
                    currentNode.OnDataChanged -= OnNodeDataChanged;
                    currentNode = null;
                }
                ShowSkillAssetInfo();
                return;
            }

            // 取消订阅旧节点的事件
            if (currentNode != null)
            {
                currentNode.OnDataChanged -= OnNodeDataChanged;
            }

            currentNode = node;
            isShowingSkillAsset = false;
            titleLabel.text = "节点属性";
            contentContainer.Clear();

            // 订阅新节点的数据变化事件
            node.OnDataChanged += OnNodeDataChanged;

            var typeLabel = new Label($"类型: {GetInspectorName(node.NodeType)}")
            {
                style = { marginBottom = 10 }
            };
            contentContainer.Add(typeLabel);

            var separator = new VisualElement
            {
                style =
                {
                    height = 1,
                    backgroundColor = new Color(0.5f, 0.5f, 0.5f),
                    marginTop = 5,
                    marginBottom = 10
                }
            };
            contentContainer.Add(separator);

            currentInspector = NodeInspectorFactory.CreateInspector(node);
            if (currentInspector != null)
            {
                // 确保检查器有正确的context
                currentInspector.SetContext(graphView, graphData, filePath);
                currentInspector.BuildUI(contentContainer, node);
            }
        }

        /// <summary>
        /// 当节点数据变化时，刷新属性面板
        /// </summary>
        private void OnNodeDataChanged()
        {
            if (currentNode != null)
            {
                UpdateSelection(currentNode);
                OnNodeDataModified?.Invoke();
            }
        }

        /// <summary>
        /// 获取枚举的InspectorName特性值
        /// </summary>
        private string GetInspectorName<T>(T enumValue) where T : System.Enum
        {
            var fieldInfo = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = fieldInfo?.GetCustomAttribute<InspectorNameAttribute>();
            return attribute?.displayName ?? enumValue.ToString();
        }
    }
}
