using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Dumb view component for one side of <see cref="GameScreen"/>'s player
    /// info row — name, avatar, rank, piece color and an optional extra
    /// line (AI difficulty, today's only use). The same prefab-built
    /// hierarchy is reused for both the session player's panel (left) and
    /// the opponent's panel (right); <see cref="GameScreen"/> owns all the
    /// "what goes in each panel for this mode" decisions and just calls the
    /// setters here.
    /// </summary>
    public sealed class PlayerProfilePanel : MonoBehaviour
    {
        [SerializeField] private Image _avatarImage;
        [SerializeField] private TMP_Text _nameLabel;
        [SerializeField] private Image _rankIcon;
        [SerializeField] private TMP_Text _rankLabel;
        [SerializeField] private Image _pieceColorIcon;
        [SerializeField] private TMP_Text _pieceColorLabel;
        [SerializeField] private TMP_Text _extraInfoLabel;

        /// <summary>Sets the display name and avatar. Passing a null sprite hides the avatar image instead of showing a blank graphic.</summary>
        public void SetIdentity(string displayName, Sprite avatarSprite)
        {
            if (_nameLabel != null)
            {
                _nameLabel.text = displayName ?? string.Empty;
            }

            SetIconIfAvailable(_avatarImage, avatarSprite);
        }

        /// <summary>Shows the rank tier icon and label (e.g. "#3 · Estrategista").</summary>
        public void SetRank(Sprite rankIconSprite, string rankText)
        {
            SetIconIfAvailable(_rankIcon, rankIconSprite);

            if (_rankLabel != null)
            {
                _rankLabel.text = rankText ?? string.Empty;
                _rankLabel.gameObject.SetActive(!string.IsNullOrEmpty(rankText));
            }
        }

        /// <summary>Hides the rank row entirely — used for LocalPvP's second player and the VsAI persona, neither of which has a real rank.</summary>
        public void HideRank()
        {
            SetIconIfAvailable(_rankIcon, null);

            if (_rankLabel != null)
            {
                _rankLabel.text = string.Empty;
                _rankLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Shows which piece color (Black/White) this player is playing, as
        /// a plain circle tinted <paramref name="iconColor"/> — the same
        /// fallback look <see cref="BoardView"/> uses for a disc with no
        /// skin equipped, since this icon has no skin of its own to show.
        /// </summary>
        public void SetPieceColor(Sprite circleSprite, Color iconColor, string colorText)
        {
            SetIconIfAvailable(_pieceColorIcon, circleSprite);
            if (_pieceColorIcon != null)
            {
                _pieceColorIcon.color = iconColor;
            }

            if (_pieceColorLabel != null)
            {
                _pieceColorLabel.text = colorText ?? string.Empty;
            }
        }

        /// <summary>Sets the optional extra info line (e.g. AI difficulty), hiding it entirely when <paramref name="text"/> is null or empty.</summary>
        public void SetExtraInfo(string text)
        {
            if (_extraInfoLabel == null)
            {
                return;
            }

            _extraInfoLabel.text = text ?? string.Empty;
            _extraInfoLabel.gameObject.SetActive(!string.IsNullOrEmpty(text));
        }

        private static void SetIconIfAvailable(Image image, Sprite sprite)
        {
            if (image == null)
            {
                return;
            }

            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}
