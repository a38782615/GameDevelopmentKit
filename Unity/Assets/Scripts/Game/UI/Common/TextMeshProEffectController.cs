using TMPro;
using UnityEngine;

namespace Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextMeshProEffectController : MonoBehaviour
    {
        [System.Serializable]
        public struct OutlineSettings
        {
            public bool Enabled;
            [Range(0f, 1f)]
            public float Width;
            public Color Color;
        }

        [System.Serializable]
        public struct GlowSettings
        {
            public bool Enabled;
            public Color Color;
            [Range(0f, 1f)]
            public float Offset;
            [Range(0f, 1f)]
            public float Power;
        }

        [System.Serializable]
        public struct ShadowSettings
        {
            public bool Enabled;
            public Color Color;
            [Range(-1f, 1f)]
            public float OffsetX;
            [Range(-1f, 1f)]
            public float OffsetY;
            [Range(-1f, 1f)]
            public float Dilate;
            [Range(0f, 1f)]
            public float Softness;
        }

        [SerializeField]
        private TextMeshProUGUI m_TextMeshProUGUI;

        [SerializeField]
        private Material m_RuntimeMaterial;

        [SerializeField]
        private Material m_SourceMaterial;

        [SerializeField]
        private OutlineSettings m_Outline = new OutlineSettings
        {
            Enabled = false,
            Width = 0f,
            Color = Color.black
        };

        [SerializeField]
        private GlowSettings m_Glow = new GlowSettings
        {
            Enabled = false,
            Color = Color.white,
            Offset = 0f,
            Power = 0f
        };

        [SerializeField]
        private ShadowSettings m_Shadow = new ShadowSettings
        {
            Enabled = false,
            Color = Color.black,
            OffsetX = 0f,
            OffsetY = 0f,
            Dilate = 0f,
            Softness = 0f
        };

        public TextMeshProUGUI TextMeshProUGUI
        {
            get
            {
                return m_TextMeshProUGUI;
            }
        }

        public Material RuntimeMaterial
        {
            get
            {
                return m_RuntimeMaterial;
            }
        }

        public Material SourceMaterial
        {
            get
            {
                return m_SourceMaterial;
            }
        }

        public OutlineSettings Outline
        {
            get
            {
                return m_Outline;
            }
            set
            {
                m_Outline = value;
            }
        }

        public GlowSettings Glow
        {
            get
            {
                return m_Glow;
            }
            set
            {
                m_Glow = value;
            }
        }

        public ShadowSettings Shadow
        {
            get
            {
                return m_Shadow;
            }
            set
            {
                m_Shadow = value;
            }
        }

        private void Reset()
        {
            CacheText();
        }

        private void Awake()
        {
            CacheText();
        }

        private void OnEnable()
        {
            ApplyToText();
        }

        public void CaptureSourceMaterial()
        {
            CacheText();
            if (m_TextMeshProUGUI == null)
            {
                return;
            }

            Material currentMaterial = m_TextMeshProUGUI.fontSharedMaterial;
            if (currentMaterial == null)
            {
                return;
            }

            if (currentMaterial != m_RuntimeMaterial)
            {
                m_SourceMaterial = currentMaterial;
            }
            else if (m_SourceMaterial == null)
            {
                m_SourceMaterial = currentMaterial;
            }
        }

        public void SetRuntimeMaterial(Material runtimeMaterial)
        {
            m_RuntimeMaterial = runtimeMaterial;
        }

        public void ApplyToText()
        {
            CacheText();
            if (m_TextMeshProUGUI == null || m_RuntimeMaterial == null)
            {
                return;
            }

            if (m_TextMeshProUGUI.fontSharedMaterial != m_RuntimeMaterial)
            {
                m_TextMeshProUGUI.fontSharedMaterial = m_RuntimeMaterial;
            }

            m_TextMeshProUGUI.UpdateMeshPadding();
            m_TextMeshProUGUI.SetMaterialDirty();
            m_TextMeshProUGUI.SetVerticesDirty();
        }

        private void CacheText()
        {
            if (m_TextMeshProUGUI == null)
            {
                m_TextMeshProUGUI = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
