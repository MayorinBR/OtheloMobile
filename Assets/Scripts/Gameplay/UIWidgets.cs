using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Small procedural-UI helpers shared by every screen that builds its
    /// rows at runtime instead of from a fixed Editor layout — currently
    /// <see cref="ShopView"/> and <see cref="CustomizationScreen"/> — so the
    /// label/button look and setup lives in exactly one place. Uses
    /// TextMeshPro, matching the rest of the project's UI.
    /// </summary>
    public static class UIWidgets
    {
        public static TMP_Text CreateLabel(Transform parent, string text, TextAlignmentOptions alignment = TextAlignmentOptions.Left)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);

            var label = go.GetComponent<TextMeshProUGUI>();
            label.text = text;
            label.color = Color.white;
            label.alignment = alignment;
            return label;
        }

        public static Button CreateButton(Transform parent, string label)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            // A fresh RectTransform defaults to a 100x100 box centered on
            // its own anchor point rather than a layout-group-friendly
            // stretch rect. HorizontalLayoutGroup normally overrides this
            // on its next rebuild, but resetting it here removes any
            // window where the button could render at that raw default
            // position instead of where its row places it.
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            Sprite buttonSprite = CosmeticArt.LoadButton();
            if (buttonSprite != null)
            {
                image.sprite = buttonSprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                // Falls back to the original flat fill when the button art
                // hasn't been imported into the project yet.
                image.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            TMP_Text labelText = CreateLabel(go.transform, label, TextAlignmentOptions.Center);
            var labelRect = (RectTransform)labelText.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        /// <summary>
        /// A small non-interactive thumbnail, used by <see cref="ShopView"/>
        /// and <see cref="CustomizationScreen"/> to preview a cosmetic item
        /// next to its row: a soft circular card with the item's sprite
        /// centered on top, instead of the bare sprite by itself. Left
        /// disabled (invisible) when <paramref name="sprite"/> is null, so a
        /// missing asset shows an empty card rather than nothing at all.
        /// <para>
        /// The card carries a <see cref="LayoutElement"/> locking it to a
        /// fixed <paramref name="size"/> — without one, the row's
        /// <see cref="HorizontalLayoutGroup"/> has no reported preferred
        /// size for this child and silently collapses it to zero width,
        /// which is also what let the sprite itself render squashed instead
        /// of as a circle.
        /// </para>
        /// </summary>
        public static Image CreateIcon(Transform parent, Sprite sprite, float size = 64f)
        {
            var go = new GameObject("Icon", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(size, size);

            var layoutElement = go.GetComponent<LayoutElement>();
            layoutElement.minWidth = size;
            layoutElement.minHeight = size;
            layoutElement.preferredWidth = size;
            layoutElement.preferredHeight = size;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;

            var backdrop = go.GetComponent<Image>();
            backdrop.sprite = CircleSpriteFactory.GetCircleSprite();
            backdrop.color = new Color(1f, 1f, 1f, 0.12f);
            backdrop.raycastTarget = false;

            var artGo = new GameObject("Art", typeof(RectTransform), typeof(Image));
            artGo.transform.SetParent(go.transform, false);

            var artRect = (RectTransform)artGo.transform;
            artRect.anchorMin = new Vector2(0.12f, 0.12f);
            artRect.anchorMax = new Vector2(0.88f, 0.88f);
            artRect.offsetMin = Vector2.zero;
            artRect.offsetMax = Vector2.zero;

            var art = artGo.GetComponent<Image>();
            art.sprite = sprite;
            art.enabled = sprite != null;
            art.raycastTarget = false;
            art.preserveAspect = true;
            return art;
        }
    }
}
