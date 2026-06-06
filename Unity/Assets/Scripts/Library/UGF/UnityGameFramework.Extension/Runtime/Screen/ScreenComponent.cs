using GameFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Extension
{
    [InfoBox("目前只针对于UGui")]
    public sealed class ScreenComponent : GameFrameworkComponent
    {
        private const float WideScreenMatchThreshold = 750f / 1335f;

        [SerializeField]
        private CanvasScaler m_UIRootCanvasScaler;

        [SerializeField, OnValueChanged("OnDefaultStandardSizeChange"), DisableInPlayMode]
        private int m_DefaultStandardWidth;

        [SerializeField, OnValueChanged("OnDefaultStandardSizeChange"), DisableInPlayMode]
        private int m_DefaultStandardHeight;

        private RectTransform m_UIRootRectTransform;

        public CanvasScaler UIRootCanvasScaler => this.m_UIRootCanvasScaler;

        [ShowInInspector, ReadOnly]
        public int StandardWidth { private set; get; }

        [ShowInInspector, ReadOnly]
        public int StandardHeight { private set; get; }

        [ShowInInspector, ReadOnly]
        public int Width { private set; get; }

        [ShowInInspector, ReadOnly]
        public int Height { private set; get; }

        [ShowInInspector, ReadOnly]
        public Rect SafeArea { private set; get; }

        [ShowInInspector, ReadOnly]
        public float UIWidth { private set; get; }

        [ShowInInspector, ReadOnly]
        public float UIHeight { private set; get; }

        [ShowInInspector, ReadOnly]
        public float StandardVerticalRatio { private set; get; }

        [ShowInInspector, ReadOnly]
        public float StandardHorizontalRatio { private set; get; }

        public static float GetCanvasScalerMatch(float width, float height)
        {
            if (width <= 0f || height <= 0f)
            {
                return 1f;
            }

            float ratio = width / height;
            return ratio >= WideScreenMatchThreshold ? 1f : 0f;
        }

        protected override void Awake()
        {
            base.Awake();
            this.m_UIRootRectTransform = this.m_UIRootCanvasScaler.GetComponent<RectTransform>();
            Set(this.m_DefaultStandardWidth, this.m_DefaultStandardHeight);
        }

        private void Update()
        {
            Rect safeArea = Screen.safeArea;
            int width = Screen.width;
            int height = Screen.height;
            if (this.Width == width && this.Height == height && this.SafeArea == safeArea)
            {
                return;
            }

            RefreshScreenMetrics(safeArea, width, height);
        }

        public void Set(int standardWidth, int standardHeight)
        {
            this.StandardWidth = standardWidth;
            this.StandardHeight = standardHeight;
            Log.Info(Utility.Text.Format("设置屏幕标准宽高:{0} ,高:{1} .", this.StandardWidth, this.StandardHeight));
            this.m_UIRootCanvasScaler.referenceResolution = new Vector2(this.StandardWidth, this.StandardHeight);
            this.StandardVerticalRatio = 1f * this.StandardHeight / this.StandardWidth;
            this.StandardHorizontalRatio = 1f * this.StandardWidth / this.StandardHeight;
            RefreshScreenMetrics(Screen.safeArea, Screen.width, Screen.height);
        }

        private void OnDefaultStandardSizeChange()
        {
            this.m_UIRootCanvasScaler.referenceResolution = new Vector2(this.m_DefaultStandardWidth, this.m_DefaultStandardHeight);
        }

        private void RefreshScreenMetrics(Rect safeArea, int width, int height)
        {
            this.SafeArea = safeArea;
            this.Width = width;
            this.Height = height;
            Log.Info(Utility.Text.Format("设置屏幕安全区域 x:{0} ,y:{1} ,width:{2} ,height:{3} .", this.SafeArea.x, this.SafeArea.y, this.SafeArea.width, this.SafeArea.height));
            Log.Info(Utility.Text.Format("屏幕宽高:{0} ,高:{1} .", this.Width, this.Height));
            this.m_UIRootCanvasScaler.matchWidthOrHeight = GetCanvasScalerMatch(this.SafeArea.width, this.SafeArea.height);
            Canvas.ForceUpdateCanvases();
            Vector2 sizeDelta = this.m_UIRootRectTransform.sizeDelta;
            this.UIWidth = sizeDelta.x;
            this.UIHeight = sizeDelta.y;
        }
    }
}
