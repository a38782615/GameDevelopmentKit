using System;
using GameFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Extension;

namespace Game
{
    [Serializable]
    public class SpriteRendererSet : AssetSet<Sprite>
    {
        [ShowInInspector]
        private SpriteRenderer m_SpriteRenderer;
        [ShowInInspector]
        private Sprite m_CurSprite;

        public static SpriteRendererSet Create(SpriteRenderer spriteRenderer, string spritePath)
        {
            SpriteRendererSet spriteRendererSet = ReferencePool.Acquire<SpriteRendererSet>();
            spriteRendererSet.m_SpriteRenderer = spriteRenderer;
            spriteRendererSet.AssetPath = spritePath;
            spriteRendererSet.Target = spriteRenderer;
            return spriteRendererSet;
        }

        public override void SetAsset(Sprite asset)
        {
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.sprite = asset;
                m_CurSprite = asset;
            }
        }

        public override bool IsCanRelease()
        {
            return m_SpriteRenderer == null || m_SpriteRenderer.sprite != m_CurSprite && m_CurSprite != null;
        }

        public override void Clear()
        {
            base.Clear();
            m_SpriteRenderer = null;
            m_CurSprite = null;
        }
    }
}
