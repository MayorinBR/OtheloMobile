using UnityEngine;

namespace Othello.Unity
{
    /// <summary>
    /// Builds a simple placeholder avatar (a person silhouette on a colored
    /// circular background) at runtime, so <see cref="Othello.Services.InventoryCatalog.AvatarDefault"/>
    /// has a real, visible avatar the same way the default piece skin (a
    /// tinted <see cref="CircleSpriteFactory"/> disc) and the default board
    /// skin (a flat square fill) already do, with no imported art asset
    /// needed. Every other avatar id still needs real imported art (see
    /// <see cref="CosmeticArt.LoadAvatar"/>) — this only covers the one item
    /// unlocked for every player from the start, which used to render as
    /// nothing at all.
    /// </summary>
    public static class AvatarSpriteFactory
    {
        private static Sprite _cachedSprite;

        public static Sprite GetDefaultAvatarSprite(int diameter = 128)
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

            var backgroundColor = new Color(0.45f, 0.5f, 0.58f);
            var figureColor = new Color(0.88f, 0.90f, 0.94f);
            float radius = diameter / 2f;
            var center = new Vector2(radius, radius);

            // A small circle near the top for the head, and a much wider
            // circle centered below the bottom edge so only its upper arc
            // shows — reading as shoulders instead of a full body. Texture
            // rows run bottom (y=0) to top (y=diameter), so "near the top"
            // means a large y and "below the bottom edge" means a negative one.
            var headCenter = new Vector2(radius, diameter * 0.68f);
            float headRadius = diameter * 0.16f;
            var shouldersCenter = new Vector2(radius, diameter * -0.08f);
            float shouldersRadius = diameter * 0.62f;

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    var point = new Vector2(x + 0.5f, y + 0.5f);
                    float distanceFromCenter = Vector2.Distance(point, center);
                    // One pixel of soft falloff at the edge instead of a hard, jagged circle.
                    float alpha = Mathf.Clamp01(radius - distanceFromCenter);

                    bool isFigure = Vector2.Distance(point, headCenter) <= headRadius
                        || Vector2.Distance(point, shouldersCenter) <= shouldersRadius;

                    Color pixelColor = isFigure ? figureColor : backgroundColor;
                    texture.SetPixel(x, y, new Color(pixelColor.r, pixelColor.g, pixelColor.b, alpha));
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
