using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Builds a single anti-aliased white circle sprite at runtime, so the
    /// board view has a disc shape to tint per player without needing any
    /// imported art asset. The sprite is generated once and reused for every
    /// disc and move-hint dot in the scene.
    /// </summary>
    public static class CircleSpriteFactory
    {
        private static Sprite _cachedSprite;

        public static Sprite GetCircleSprite(int diameter = 128)
        {
            if (_cachedSprite != null)
            {
                return _cachedSprite;
            }

            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float radius = diameter / 2f;
            var center = new Vector2(radius, radius);

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float distanceFromCenter = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    // One pixel of soft falloff at the edge instead of a hard, jagged circle.
                    float alpha = Mathf.Clamp01(radius - distanceFromCenter);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            texture.Apply();

            _cachedSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, diameter, diameter),
                new Vector2(0.5f, 0.5f));

            return _cachedSprite;
        }
    }
}
