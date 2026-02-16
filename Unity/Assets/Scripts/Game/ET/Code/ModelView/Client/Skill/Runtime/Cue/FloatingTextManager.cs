using UnityEngine;
using UnityEngine.UI;
using TMPro;


namespace ET.Client
{
    /// <summary>
    /// 飘字管理器 - 负责创建和管理飘字
    /// 需要挂载到场景中，并配置 Canvas 和 Camera
    /// </summary>
    [EnableClass]
    public class FloatingTextManager : MonoBehaviour
    {
        [StaticField]
        private static FloatingTextManager _instance;
        public static FloatingTextManager Instance => _instance;

        [Header("必要配置")]
        [Tooltip("用于显示飘字的 Screen Space Canvas")]
        public Canvas floatingTextCanvas;

        [Tooltip("游戏主相机")]
        public Camera gameCamera;

        [Header("飘字预制体（可选）")]
        [Tooltip("自定义飘字预制体，需要有 TextMeshProUGUI 组件")]
        public GameObject floatingTextPrefab;

        [Header("默认设置")]
        public float defaultFontSize = 36f;
        public float defaultDuration = 1.2f;
        public Vector2 defaultMoveDistance = new Vector2(0, 80f);

        [Header("随机弧度设置")]
        [Tooltip("水平随机偏移范围")]
        public float horizontalRandomRange = 60f;
        [Tooltip("初始弹出力度")]
        public float popForce = 150f;
        [Tooltip("重力")]
        public float gravity = 300f;

        private void Awake()
        {
            _instance = this;

            if (gameCamera == null)
            {
                gameCamera = Camera.main;
            }

            if (floatingTextCanvas == null)
            {
                CreateDefaultCanvas();
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// 创建默认 Canvas
        /// </summary>
        private void CreateDefaultCanvas()
        {
            var canvasGO = new GameObject("FloatingTextCanvas");
            canvasGO.transform.SetParent(transform);

            floatingTextCanvas = canvasGO.AddComponent<Canvas>();
            floatingTextCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            floatingTextCanvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        /// <summary>
        /// 创建飘字
        /// </summary>
        public GameObject CreateFloatingText(
            string text,
            Vector3 worldPosition,
            Color color,
            float fontSize,
            float duration,
            FloatingTextType textType)
        {
            if (floatingTextCanvas == null || gameCamera == null)
            {
                Debug.LogWarning("[FloatingTextManager] Canvas 或 Camera 未配置");
                return null;
            }

            // 世界坐标转屏幕坐标
            Vector3 screenPos = gameCamera.WorldToScreenPoint(worldPosition);

            // 如果在相机后面，不显示
            if (screenPos.z < 0)
                return null;

            // 创建飘字对象
            GameObject floatingTextGO;
            if (floatingTextPrefab != null)
            {
                floatingTextGO = Instantiate(floatingTextPrefab, floatingTextCanvas.transform);
            }
            else
            {
                return null;
            }

            if (floatingTextGO == null)
                return null;

            // 设置位置（屏幕坐标）
            var rectTransform = floatingTextGO.GetComponent<RectTransform>();
            rectTransform.position = screenPos;

            // 设置文本
            var textComponent = floatingTextGO.GetComponent<TextMeshProUGUI>();
            if (textComponent == null)
            {
                textComponent = floatingTextGO.GetComponentInChildren<TextMeshProUGUI>();
            }

            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = GetColorByType(textType, color);
                textComponent.fontSize = fontSize > 0 ? fontSize : defaultFontSize;
            }

            // 添加飘字动画
            var animator = floatingTextGO.AddComponent<FloatingTextAnimator>();
            animator.Initialize(
                duration > 0 ? duration : defaultDuration,
                horizontalRandomRange,
                popForce,
                gravity
            );

            return floatingTextGO;
        }



        /// <summary>
        /// 根据飘字类型获取颜色
        /// </summary>
        private Color GetColorByType(FloatingTextType textType, Color customColor)
        {
            // 如果指定了自定义颜色（非白色），使用自定义颜色
            if (customColor != Color.white)
                return customColor;

            switch (textType)
            {
                case FloatingTextType.Damage:
                    return new Color(1f, 0.3f, 0.3f); // 红色
                case FloatingTextType.Heal:
                    return new Color(0.3f, 1f, 0.3f); // 绿色
                case FloatingTextType.Status:
                    return new Color(1f, 1f, 0.3f); // 黄色
                case FloatingTextType.Experience:
                    return new Color(0.3f, 1f, 1f); // 青色
                case FloatingTextType.Gold:
                    return new Color(1f, 0.84f, 0f); // 金色
                default:
                    return Color.white;
            }
        }
    }


}
