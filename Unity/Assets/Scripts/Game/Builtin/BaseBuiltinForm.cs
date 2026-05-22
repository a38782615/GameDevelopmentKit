using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Extension;

namespace Game
{
    public abstract class BaseBuiltinForm : MonoBehaviour
    {
        [SerializeField] private CanvasScaler m_CanvasScaler;

        private Rect m_LastSafeArea;
        private int m_LastScreenWidth;
        private int m_LastScreenHeight;

        protected virtual void Awake()
        {
            m_CanvasScaler = gameObject.GetOrAddComponent<CanvasScaler>();
            RefreshCanvasScalerMatch();
        }

        protected virtual void Update()
        {
            Rect safeArea = Screen.safeArea;
            int width = Screen.width;
            int height = Screen.height;
            if (m_LastSafeArea == safeArea && m_LastScreenWidth == width && m_LastScreenHeight == height)
            {
                return;
            }

            RefreshCanvasScalerMatch();
        }

        protected virtual void Close()
        {
            GameObject go = gameObject;
            go.SetActive(false);
            Destroy(go);
        }

        private void RefreshCanvasScalerMatch()
        {
            Rect safeArea = Screen.safeArea;
            m_LastSafeArea = safeArea;
            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;
            m_CanvasScaler.matchWidthOrHeight = ScreenComponent.GetCanvasScalerMatch(safeArea.width, safeArea.height);
        }
    }
}
