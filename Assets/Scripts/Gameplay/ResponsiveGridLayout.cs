using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Keeps an 8x8 (or any N x N) <see cref="GridLayoutGroup"/> exactly
    /// square and sized to fill whatever space its parent gives it, on any
    /// screen. Pair this with an <see cref="AspectRatioFitter"/> (Aspect
    /// Mode = Fit In Parent, Aspect Ratio = 1) on the same RectTransform:
    /// the fitter picks the largest square that fits the parent, and this
    /// component recomputes the per-cell size whenever that square's
    /// dimensions change, instead of relying on a fixed pixel size that
    /// would only look right on the screen it was tuned for.
    /// <para>
    /// Unity calls <see cref="OnRectTransformDimensionsChange"/>
    /// automatically whenever this RectTransform's size changes — including
    /// on device rotation and on window resize in the Editor — so no manual
    /// polling or resize event wiring is needed.
    /// </para>
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class ResponsiveGridLayout : MonoBehaviour
    {
        [SerializeField]
        private int _columns = 8;

        [SerializeField]
        private int _rows = 8;

        [SerializeField]
        private float _spacing = 2f;

        private RectTransform _rectTransform;
        private GridLayoutGroup _gridLayoutGroup;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _gridLayoutGroup = GetComponent<GridLayoutGroup>();
            _gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayoutGroup.constraintCount = _columns;
            _gridLayoutGroup.spacing = new Vector2(_spacing, _spacing);

            Recalculate();
        }

        private void OnRectTransformDimensionsChange()
        {
            // Unity can fire this in edit mode before Awake has run once.
            if (_gridLayoutGroup == null)
            {
                return;
            }
            Recalculate();
        }

        private void Recalculate()
        {
            Rect size = _rectTransform.rect;

            // The AspectRatioFitter already guarantees width == height (a
            // square), so either dimension gives the same cell size — using
            // the smaller of the two is just a safety margin if that
            // assumption is ever broken (e.g. the fitter component removed).
            float side = Mathf.Min(size.width, size.height);

            float cellWidth = (side - _spacing * (_columns - 1)) / _columns;
            float cellHeight = (side - _spacing * (_rows - 1)) / _rows;
            float cellSize = Mathf.Max(1f, Mathf.Min(cellWidth, cellHeight));

            _gridLayoutGroup.cellSize = new Vector2(cellSize, cellSize);
        }
    }
}
