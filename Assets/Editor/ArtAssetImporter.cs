using UnityEditor;
using UnityEngine;

namespace Othello.EditorTools
{
    /// <summary>
    /// Forces every texture under <c>Assets/Resources/Art</c> to import as a
    /// UI sprite with a transparent background, regardless of the project's
    /// default texture import settings — so <see cref="Resources.Load{T}"/>
    /// finds a usable <see cref="Sprite"/> the moment an art file is added,
    /// with no per-file Inspector setup needed.
    /// <para>
    /// The shared button texture additionally gets a 9-slice border, so
    /// <c>Image.Type.Sliced</c> keeps its rounded corners crisp at any
    /// button size instead of stretching them.
    /// </para>
    /// </summary>
    public sealed class ArtAssetImporter : AssetPostprocessor
    {
        private const string ArtRootPath = "Assets/Resources/Art/";
        private const string ButtonFileName = "button.png";

        private void OnPreprocessTexture()
        {
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith(ArtRootPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            if (normalizedPath.EndsWith(ButtonFileName))
            {
                // (left, bottom, right, top) margin around the rounded
                // corners and their drop shadow, wide enough that 9-slicing
                // never stretches them — the flat middle does all the
                // stretching instead.
                importer.spriteBorder = new Vector4(90f, 70f, 90f, 70f);
            }
        }
    }
}
