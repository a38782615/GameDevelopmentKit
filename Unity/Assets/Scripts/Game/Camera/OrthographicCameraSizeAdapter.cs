using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game
{
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class OrthographicCameraSizeAdapter : MonoBehaviour
    {
        [SerializeField]
        private Vector2Int m_DesignResolution = new Vector2Int(750, 1335);

        [SerializeField]
        private float m_DesignOrthographicSize = 5f;

        private Camera m_CachedCamera;
        private int m_LastScreenWidth;
        private int m_LastScreenHeight;

        private void Awake()
        {
            m_CachedCamera = GetComponent<Camera>();
            ApplyCameraSize();
        }

        private void OnEnable()
        {
            ApplyCameraSize();
        }

        private void Update()
        {
            if (m_LastScreenWidth == Screen.width && m_LastScreenHeight == Screen.height)
            {
                return;
            }

            ApplyCameraSize();
        }

        private void OnValidate()
        {
            if (m_DesignResolution.x < 1)
            {
                m_DesignResolution.x = 1;
            }

            if (m_DesignResolution.y < 1)
            {
                m_DesignResolution.y = 1;
            }

            if (m_DesignOrthographicSize < 0.01f)
            {
                m_DesignOrthographicSize = 0.01f;
            }

            if (!isActiveAndEnabled)
            {
                return;
            }

            m_CachedCamera = GetComponent<Camera>();
            ApplyCameraSize();
        }

        private void ApplyCameraSize()
        {
            if (m_CachedCamera == null)
            {
                m_CachedCamera = GetComponent<Camera>();
            }

            if (m_CachedCamera == null)
            {
                return;
            }

            if (!m_CachedCamera.orthographic)
            {
                Log.Warning("OrthographicCameraSizeAdapter requires an orthographic camera.");
                return;
            }

            m_LastScreenWidth = Mathf.Max(Screen.width, 1);
            m_LastScreenHeight = Mathf.Max(Screen.height, 1);

            float widthRatio = m_LastScreenWidth / (float)m_DesignResolution.x;
            float heightRatio = m_LastScreenHeight / (float)m_DesignResolution.y;
            float resolutionRatio = Mathf.Min(widthRatio, heightRatio);
            float scale = resolutionRatio < 1f ? 1f / resolutionRatio : 1f;
            m_CachedCamera.orthographicSize = m_DesignOrthographicSize * scale;
        }
    }
}
