using UnityEngine;
using UnityGameFramework.Runtime;

namespace Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public sealed class OrthographicCameraSizeAdapter : MonoBehaviour
    {
        [SerializeField]
        private Vector2 m_DesignResolution = new Vector2(750, 1335);

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

            if(m_LastScreenWidth == Screen.width && m_LastScreenHeight == Screen.height)
            {
                return;
            }
            m_LastScreenWidth = Screen.width;
            m_LastScreenHeight = Screen.height;
            var desRatio = m_DesignResolution.x * 1.0f / m_DesignResolution.y;
            var ratio = m_LastScreenWidth * 1.0f / m_LastScreenHeight; 
            float scale = ratio >= desRatio ? 1f : (desRatio/ ratio);
            m_CachedCamera.orthographicSize = m_DesignOrthographicSize * scale;
        }
    }
}
