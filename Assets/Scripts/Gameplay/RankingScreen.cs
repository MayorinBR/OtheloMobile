using System.Collections.Generic;
using System.Linq;
using Othello.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Othello.Unity
{
    /// <summary>
    /// Shows the player's own online <see cref="PlayerSession.Rating"/>,
    /// rank tier and position, and where they currently place among
    /// <see cref="ILeaderboardService"/>'s ranked standings — each row also
    /// carries its own rank tier icon (<see cref="CosmeticArt.LoadRankIcon"/>),
    /// from <see cref="LocalizationKey.RankTierAprendiz"/> up to
    /// <see cref="LocalizationKey.RankTierChampion"/> for whoever is #1.
    /// Rebuilt every time the screen is shown, the same way
    /// <see cref="ShopView"/> and <see cref="CustomizationScreen"/> rebuild
    /// their own procedural lists.
    /// </summary>
    public sealed class RankingScreen : UIScreen
    {
        private const int LeaderboardWindowSize = 11;

        [SerializeField]
        private PlayerSessionController _playerSession;

        [SerializeField]
        private LeaderboardController _leaderboard;

        [SerializeField]
        private LocalizationController _localization;

        [SerializeField]
        private RectTransform _listContainer;

        [SerializeField]
        private TMP_Text _yourRatingLabel;

        [SerializeField]
        private Image _yourRankIcon;

        [SerializeField]
        private TMP_Text _yourRankLabel;

        [SerializeField]
        private Button _backButton;

        private readonly List<GameObject> _spawnedRows = new();

        private void Awake()
        {
            _backButton?.onClick.AddListener(() => Manager.GoBack());
        }

        protected override void OnShown() => Rebuild();

        private void Rebuild()
        {
            foreach (var row in _spawnedRows)
            {
                Destroy(row);
            }
            _spawnedRows.Clear();

            if (_playerSession == null || _playerSession.Session == null || _leaderboard == null || _listContainer == null)
            {
                return;
            }

            PlayerSession session = _playerSession.Session;

            if (_yourRatingLabel != null)
            {
                _yourRatingLabel.text = string.Format(
                    _localization.Service.Get(LocalizationKey.RankingYourRatingFormat), session.Rating);
            }

            // The local mock invokes this synchronously, but a real backend
            // query would not — this screen may no longer be the active one
            // by the time it completes, so the rows are only spawned if it's
            // still showing.
            _leaderboard.Service.GetRanked(session.DisplayName, session.Rating, LeaderboardWindowSize, entries =>
            {
                if (this == null || !gameObject.activeInHierarchy)
                {
                    return;
                }

                LeaderboardEntry ownEntry = entries.FirstOrDefault(entry => entry.IsLocalPlayer);
                if (ownEntry != null)
                {
                    ShowYourRank(ownEntry);
                }

                foreach (var entry in entries)
                {
                    _spawnedRows.Add(BuildRow(entry));
                }
            });
        }

        private void ShowYourRank(LeaderboardEntry ownEntry)
        {
            if (_yourRankIcon != null)
            {
                Sprite icon = CosmeticArt.LoadRankIcon(ownEntry.Tier);
                _yourRankIcon.sprite = icon;
                _yourRankIcon.enabled = icon != null;
            }

            if (_yourRankLabel != null)
            {
                string tierName = _localization.Service.Get(RankTierCatalog.GetNameKey(ownEntry.Tier));
                _yourRankLabel.text = string.Format(
                    _localization.Service.Get(LocalizationKey.RankTierPositionFormat), tierName, ownEntry.Rank);
            }
        }

        private GameObject BuildRow(LeaderboardEntry entry)
        {
            var row = new GameObject($"Rank_{entry.Rank}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_listContainer, false);

            // Same fix as ShopView/CustomizationScreen.BuildRow: an
            // unconfigured HorizontalLayoutGroup leaves the icon/label at
            // their own uncontrolled size.
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childAlignment = TextAnchor.MiddleLeft;
            rowLayout.spacing = 12f;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 56f;
            rowElement.flexibleWidth = 1f;
            rowElement.flexibleHeight = 0f;

            var icon = UIWidgets.CreateIcon(row.transform, CosmeticArt.LoadRankIcon(entry.Tier), size: 40f);
            var iconElement = icon.gameObject.AddComponent<LayoutElement>();
            iconElement.preferredWidth = 40f;
            iconElement.minWidth = 40f;

            // The local player's own row is marked with a leading arrow
            // instead of a font-weight change, so it stands out identically
            // regardless of which font asset the project ends up using.
            string marker = entry.IsLocalPlayer ? "> " : "";
            TMP_Text label = UIWidgets.CreateLabel(row.transform, $"{marker}{entry.Rank}. {entry.DisplayName} — {entry.Rating}");
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.flexibleWidth = 1f;
            labelElement.minWidth = 100f;

            return row;
        }
    }
}
