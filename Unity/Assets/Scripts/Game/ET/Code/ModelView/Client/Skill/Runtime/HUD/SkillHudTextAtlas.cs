using System.Collections.Generic;

using TMPro;

using UnityEngine;
using UnityEngine.TextCore;

namespace ET.Client
{
    [EnableClass]
    public sealed class SkillHudTextAtlas
    {
        public readonly struct GlyphInfo
        {
            public readonly Vector4 UvRect;
            public readonly float BearingX;
            public readonly float BearingY;
            public readonly float Width;
            public readonly float Height;
            public readonly float Advance;

            public GlyphInfo(Vector4 uvRect, float bearingX, float bearingY, float width, float height, float advance)
            {
                UvRect = uvRect;
                BearingX = bearingX;
                BearingY = bearingY;
                Width = width;
                Height = height;
                Advance = advance;
            }
        }

        private const float WorldScaleFactor = 0.012f;

        private readonly Dictionary<char, GlyphInfo> glyphCache = new Dictionary<char, GlyphInfo>();

        public TMP_FontAsset FontAsset { get; private set; }

        public Texture AtlasTexture
        {
            get
            {
                if (FontAsset == null || FontAsset.atlasTextures == null || FontAsset.atlasTextures.Length == 0)
                {
                    return null;
                }

                return FontAsset.atlasTextures[0];
            }
        }

        public bool IsReady => FontAsset != null && AtlasTexture != null;

        public bool EnsureReady()
        {
            if (IsReady)
            {
                return true;
            }

            FontAsset = TMP_Settings.defaultFontAsset;
            return IsReady;
        }

        public void EnsureCharacters(string text)
        {
            if (!EnsureReady() || string.IsNullOrEmpty(text))
            {
                return;
            }

            FontAsset.TryAddCharacters(text, out _);
        }

        public bool TryGetGlyph(char character, out GlyphInfo glyphInfo)
        {
            if (glyphCache.TryGetValue(character, out glyphInfo))
            {
                return true;
            }

            glyphInfo = default;
            if (!EnsureReady())
            {
                return false;
            }

            if (!FontAsset.characterLookupTable.TryGetValue((uint)character, out TMP_Character characterInfo))
            {
                return false;
            }

            Glyph glyph = characterInfo.glyph;
            if (glyph == null)
            {
                return false;
            }

            Texture atlasTexture = AtlasTexture;
            if (atlasTexture == null || atlasTexture.width <= 0 || atlasTexture.height <= 0)
            {
                return false;
            }

            GlyphRect glyphRect = glyph.glyphRect;
            Vector4 uvRect = new Vector4(
                glyphRect.x / atlasTexture.width,
                glyphRect.y / atlasTexture.height,
                glyphRect.width / (float)atlasTexture.width,
                glyphRect.height / (float)atlasTexture.height);

            GlyphMetrics metrics = glyph.metrics;
            glyphInfo = new GlyphInfo(
                uvRect,
                metrics.horizontalBearingX,
                metrics.horizontalBearingY,
                metrics.width,
                metrics.height,
                metrics.horizontalAdvance);
            glyphCache[character] = glyphInfo;
            return true;
        }

        public float GetWorldScale(float fontSize)
        {
            if (!EnsureReady())
            {
                return 0f;
            }

            float pointSize = FontAsset.faceInfo.pointSize;
            if (pointSize <= 0.01f)
            {
                pointSize = 90f;
            }

            return fontSize / pointSize * WorldScaleFactor;
        }

        public float GetLineHeight(float fontSize)
        {
            if (!EnsureReady())
            {
                return 0f;
            }

            return FontAsset.faceInfo.lineHeight * GetWorldScale(fontSize);
        }
    }
}
