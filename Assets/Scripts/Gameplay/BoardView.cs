using System.Linq;
using Othello.Core;
using Othello.Services;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Procedural 8x8 board view: builds one cell per square under
    /// <see cref="_boardGrid"/> the first time it is bound, then keeps them
    /// in sync with whichever match <see cref="GameController"/> is
    /// currently running. A tap goes straight to
    /// <see cref="GameController.TryPlayHumanMove"/> — this class has no
    /// notion of Othello's rules, it only reflects whatever
    /// <see cref="BoardState"/> the controller currently holds.
    /// <para>
    /// Editor setup: put a Canvas in the scene, add an empty RectTransform
    /// under it with a <c>Grid Layout Group</c> configured for 8 columns,
    /// and assign that RectTransform as <see cref="_boardGrid"/>. Assign
    /// this component to <see cref="GameScreen"/>, which calls
    /// <see cref="Bind"/> every time a new match starts.
    /// </para>
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _boardGrid;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private Color _darkSquareColor = new(0.11f, 0.35f, 0.24f);

        [SerializeField]
        private Color _lightSquareColor = new(0.15f, 0.43f, 0.30f);

        [SerializeField]
        private Color _validMoveHintColor = new(1f, 1f, 1f, 0.25f);

        [SerializeField]
        private Color _blackDiscColor = Color.black;

        [SerializeField]
        private Color _whiteDiscColor = Color.white;

        private readonly Image[] _discs = new Image[64];
        private readonly Image[] _validMoveHints = new Image[64];
        private readonly Image[] _squares = new Image[64];

        private GameController _controller;
        private OthelloGame _boundGame;

        /// <summary>
        /// Points this view at <paramref name="controller"/>'s current
        /// match: builds the 64 cells on the first call, and on every call
        /// re-subscribes to the (possibly new) <see cref="OthelloGame"/>
        /// instead of leaking a subscription to whatever match ended before.
        /// </summary>
        public void Bind(GameController controller)
        {
            if (_boardGrid == null)
            {
                Debug.LogError("BoardView precisa de _boardGrid atribuído no Inspector.");
                return;
            }

            _controller = controller;

            if (_discs[0] == null)
            {
                BuildCells();
            }

            RebindGameEvents();
            Refresh();
        }

        private void RebindGameEvents()
        {
            if (_boundGame != null)
            {
                _boundGame.MoveApplied -= OnMoveApplied;
                _boundGame.GameEnded -= Refresh;
            }

            _boundGame = _controller.Game;
            _boundGame.MoveApplied += OnMoveApplied;
            _boundGame.GameEnded += Refresh;
        }

        private void OnMoveApplied(int square, Player mover) => Refresh();

        private void BuildCells()
        {
            Sprite discSprite = CircleSpriteFactory.GetCircleSprite();

            for (int square = 0; square < 64; square++)
            {
                int row = square / 8;
                int col = square % 8;
                bool isDarkSquare = (row + col) % 2 == 0;

                var cell = new GameObject($"Cell_{square}", typeof(RectTransform), typeof(Image), typeof(Button));
                cell.transform.SetParent(_boardGrid, false);

                var squareImage = cell.GetComponent<Image>();
                squareImage.color = isDarkSquare ? _darkSquareColor : _lightSquareColor;
                _squares[square] = squareImage;

                var hint = CreateChildImage(cell.transform, "ValidMoveHint", discSprite, marginFraction: 0.35f);
                hint.color = _validMoveHintColor;
                hint.enabled = false;
                _validMoveHints[square] = hint;

                var disc = CreateChildImage(cell.transform, "Disc", discSprite, marginFraction: 0.1f);
                disc.enabled = false;
                _discs[square] = disc;

                int capturedSquare = square; // each cell must close over its own square, not the loop variable
                cell.GetComponent<Button>().onClick.AddListener(() => _controller.TryPlayHumanMove(capturedSquare));
            }
        }

        private static Image CreateChildImage(Transform parent, string name, Sprite sprite, float marginFraction)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(marginFraction, marginFraction);
            rect.anchorMax = new Vector2(1f - marginFraction, 1f - marginFraction);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }

        private void Refresh()
        {
            BoardState state = _controller.Game.State;
            PlayerSession session = _playerSession != null ? _playerSession.Session : null;

            ApplyBoardSkin(session?.EquippedBoardSkinId);

            Sprite defaultDiscSprite = CircleSpriteFactory.GetCircleSprite();
            Sprite blackPieceSprite = CosmeticArt.LoadPiece(session?.EquippedPieceSkinId, Player.Black);
            Sprite whitePieceSprite = CosmeticArt.LoadPiece(session?.EquippedPieceSkinId, Player.White);

            // In local pass-and-play both sides are human, so hints show on
            // every turn; against the AI they only show on the human's turn.
            bool showHints = !_controller.Game.IsGameOver &&
                (_controller.Mode == GameMode.LocalPvP || state.CurrentPlayer == _controller.HumanPlayer);
            var validMoves = showHints ? _controller.Game.GetValidMoves() : null;

            for (int square = 0; square < 64; square++)
            {
                ulong bit = 1UL << square;
                bool blackHere = (state.Black & bit) != 0;
                bool whiteHere = (state.White & bit) != 0;

                _discs[square].enabled = blackHere || whiteHere;
                if (blackHere)
                {
                    ApplyDiscLook(_discs[square], blackPieceSprite, defaultDiscSprite, _blackDiscColor);
                }
                else if (whiteHere)
                {
                    ApplyDiscLook(_discs[square], whitePieceSprite, defaultDiscSprite, _whiteDiscColor);
                }

                _validMoveHints[square].enabled = validMoves != null && validMoves.Contains(square);
            }
        }

        /// <summary>
        /// Points every square at the equipped board skin's texture, falling
        /// back to the plain <see cref="_darkSquareColor"/>/<see cref="_lightSquareColor"/>
        /// fill when nothing is equipped or the skin's art hasn't been
        /// imported into the project yet.
        /// </summary>
        private void ApplyBoardSkin(string boardSkinId)
        {
            for (int square = 0; square < 64; square++)
            {
                bool isDarkSquare = (square / 8 + square % 8) % 2 == 0;
                Sprite skinSprite = CosmeticArt.LoadBoardSquare(boardSkinId, isDarkSquare);

                if (skinSprite != null)
                {
                    _squares[square].sprite = skinSprite;
                    _squares[square].color = Color.white;
                }
                else
                {
                    _squares[square].sprite = null;
                    _squares[square].color = isDarkSquare ? _darkSquareColor : _lightSquareColor;
                }
            }
        }

        /// <summary>
        /// Uses the equipped piece skin's sprite at full color when one is
        /// available, otherwise falls back to the procedural circle tinted
        /// per player — the original look, unchanged for anyone who hasn't
        /// equipped a skin.
        /// </summary>
        private static void ApplyDiscLook(Image disc, Sprite skinSprite, Sprite fallbackSprite, Color fallbackColor)
        {
            if (skinSprite != null)
            {
                disc.sprite = skinSprite;
                disc.color = Color.white;
            }
            else
            {
                disc.sprite = fallbackSprite;
                disc.color = fallbackColor;
            }
        }
    }
}
