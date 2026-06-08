using System;
using Cysharp.Threading.Tasks;
using GameFramework;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Extension;

namespace Game
{
    [Serializable]
    public class WaitableSpriteRendererSet : AssetSet<Sprite>
    {
        [ShowInInspector]
        private SpriteRenderer m_SpriteRenderer;
        [ShowInInspector]
        private Sprite m_CurSprite;

        private AutoResetUniTaskCompletionSource m_Tcs;

        public static WaitableSpriteRendererSet Create(SpriteRenderer spriteRenderer, string spritePath, AutoResetUniTaskCompletionSource tcs)
        {
            WaitableSpriteRendererSet waitableSpriteRendererSet = ReferencePool.Acquire<WaitableSpriteRendererSet>();
            waitableSpriteRendererSet.m_SpriteRenderer = spriteRenderer;
            waitableSpriteRendererSet.m_Tcs = tcs;
            waitableSpriteRendererSet.AssetPath = spritePath;
            waitableSpriteRendererSet.Target = spriteRenderer;
            return waitableSpriteRendererSet;
        }

        public override void SetAsset(Sprite asset)
        {
            if (m_SpriteRenderer != null)
            {
                m_SpriteRenderer.sprite = asset;
                m_CurSprite = asset;
            }

            if (m_Tcs != null)
            {
                m_Tcs.TrySetResult();
                m_Tcs = null;
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
            if (m_Tcs != null)
            {
                m_Tcs.TrySetCanceled();
                m_Tcs = null;
            }
        }
    }
}
